using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Options;
using MockHttpDemo.Application.Config;
using MockHttpDemo.Application.Dtos;

namespace MockHttpDemo.Application.Services;

public class MovieService : IMovieService
{
    private readonly HttpClient _httpClient;
    private readonly MovieServiceConfig _config;

    private TokenResponse? _cachedToken;

    public MovieService(
        IHttpClientFactory httpClientFactory,
        IOptions<MovieServiceConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient("MovieClient");
        _config = config.Value;

        _httpClient.BaseAddress = _config.BaseUri;
    }
    
    public async Task<ICollection<Movie>> SearchAsync(SearchMoviesRequest request)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (request.Title is not null)
        {
            query.Add("title", request.Title);
        }

        if (request.Year is not null)
        {
            query.Add("year", request.Year.Value.ToString());
        }

        var queryString = query.ToString();
        const string baseUri = "/api/movies";

        var uri = string.IsNullOrWhiteSpace(queryString)
            ? baseUri
            : string.Join("?", baseUri, query.ToString());
        
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        await SetupAuthAsync(message);

        var result = await _httpClient.SendAsync(message);

        if (!result.IsSuccessStatusCode)
        {
            throw new ApplicationException("Failed to search for movies");
        }

        var responseData = await result.Content.ReadFromJsonAsync<List<Movie>>();
        if (responseData is null)
        {
            throw new ApplicationException("Failed to deserialize movies");
        }

        return responseData;
    }

    public async Task<int> AddAsync(AddMovieRequest request)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/movies");
        await SetupAuthAsync(message);

        var result = await _httpClient.SendAsync(message);

        if (!result.IsSuccessStatusCode)
        {
            throw new ApplicationException("Failed to add movie");
        }

        var responseData = await result.Content.ReadFromJsonAsync<Movie>();
        if (responseData is null)
        {
            throw new ApplicationException("Failed to deserialize movie");
        }

        return responseData.Id;
    }

    private async Task SetupAuthAsync(HttpRequestMessage message)
    {
        var token = await GetAccessTokenAsync();

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedToken.Expiry)
        {
            return _cachedToken.Token;
        }

        var tokenParameters = new Dictionary<string, string>
        {
            { "client_id", _config.ClientId },
            { "client_secret", _config.ClientSecret },
            { "grant_type", "client_credentials" }
        };
        
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/token")
        {
            Content = new FormUrlEncodedContent(tokenParameters)
        };
        
        var result = await _httpClient.SendAsync(tokenRequest);
        if (!result.IsSuccessStatusCode)
        {
            throw new ApplicationException("Error getting a token");
        }

        try
        {
            var responseData = await result.Content.ReadFromJsonAsync<TokenResponse>();

            _cachedToken = responseData!;

            return responseData!.Token;
        }
        catch (Exception e)
        {
            throw new ApplicationException("Error reading token response", e);
        }
    }
}