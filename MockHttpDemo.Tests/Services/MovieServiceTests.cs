using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using MockHttpDemo.Application.Config;
using MockHttpDemo.Application.Dtos;
using MockHttpDemo.Application.Services;
using Moq;
using RichardSzalay.MockHttp;

namespace MockHttpDemo.Tests.Services;

public class MovieServiceTests : IDisposable
{
    private readonly MovieService _target;
    
    private readonly MockHttpMessageHandler _mockHttp;
    
    private readonly HttpClient _httpClient;

    private readonly MovieServiceConfig _config;

    public MovieServiceTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(factory => factory.CreateClient(It.Is<string>(x => x == "MovieClient")))
            .Returns(_httpClient);

        _config = new MovieServiceConfig(
            new Uri("http://localhost"),
            "my-client-id",
            "my-client-secret");

        var optionsMock = new Mock<IOptions<MovieServiceConfig>>();
        optionsMock
            .Setup(options => options.Value)
            .Returns(_config);

        _target = new MovieService(httpClientFactoryMock.Object, optionsMock.Object);
    }
    
    [Fact]
    public async Task Add_movie_request_succeeds_using_when()
    {
        SetupValidAuthRequestUsingWhen();

        var addRequest = _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        await _target.AddAsync(new("My Movie", 2023));

        _mockHttp
            .GetMatchCount(addRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Add_movie_request_succeeds_using_expect()
    {
        SetupValidAuthRequestUsingExpect();

        _mockHttp
            .Expect(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        await _target.AddAsync(new("My Movie", 2023));

        _mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task Auth_request_is_made_before_api_call()
    {
        var expiry = DateTime.UtcNow.AddMinutes(30);
        var tokenResponse = new TokenResponse("my-token", expiry);

        var authRequest = _mockHttp
            .When("/auth/token")
            .RespondWithJsonObject(tokenResponse);

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        await _target.AddAsync(new("My Movie", 2023));

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Auth_request_is_made_with_correct_OAuth_parameters()
    {
        var expiry = DateTime.UtcNow.AddMinutes(30);
        var tokenResponse = new TokenResponse("my-token", expiry);
        
        var authRequest = _mockHttp
            .When("/auth/token")
            .WithExactFormData(new Dictionary<string, string>
            {
                { "client_id", _config.ClientId },
                { "client_secret", _config.ClientSecret },
                { "grant_type", "client_credentials" }
            })
            .RespondWithJsonObject(tokenResponse);

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        await _target.AddAsync(new("My Movie", 2023));

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Exception_is_thrown_if_auth_request_fails()
    {
        var authRequest = _mockHttp
            .When("/auth/token")
            .Respond(HttpStatusCode.Unauthorized);

        Func<Task> act = () => _target.AddAsync(new("My Movie", 2023));

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Error getting a token");

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Exception_is_thrown_if_auth_response_cannot_be_deserialized()
    {
        var authRequest = _mockHttp
            .When("/auth/token")
            .Respond("application/json", "some bad json");

        Func<Task> act = () => _target.AddAsync(new("My Movie", 2023));

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Error reading token response");

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Auth_request_is_not_made_again_if_token_is_not_expired()
    {
        var expiry = DateTime.UtcNow.AddMinutes(30);
        var tokenResponse = new TokenResponse("my-token", expiry);

        var authRequest = _mockHttp
            .When("/auth/token")
            .RespondWithJsonObject(tokenResponse);

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        await _target.AddAsync(new("My Movie", 2023));
        await _target.AddAsync(new("My Other Movie", 2023));

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Auth_request_is_made_again_if_token_is_expired()
    {
        var expiry = DateTime.UtcNow.AddMinutes(-5);
        var tokenResponse = new TokenResponse("my-token", expiry);

        var authRequest = _mockHttp
            .When("/auth/token")
            .RespondWithJsonObject(tokenResponse);

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");

        await _target.AddAsync(new("My Movie", 2023));
        await _target.AddAsync(new("My Other Movie", 2023));

        _mockHttp
            .GetMatchCount(authRequest)
            .Should()
            .Be(2);
    }
    
    [Fact]
    public async Task Exception_is_thrown_if_add_movie_request_fails()
    {
        SetupValidAuthRequestUsingWhen();

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        Func<Task> act = () => _target.AddAsync(new("My Movie", 2023));

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Failed to add movie");
    }
    
    [Fact]
    public async Task Exception_is_thrown_if_add_movie_response_cannot_be_deserialized()
    {
        SetupValidAuthRequestUsingWhen();

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        Func<Task> act = () => _target.AddAsync(new("My Movie", 2023));

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Failed to deserialize movie");
    }
    
    [Fact]
    public async Task Add_movie_request_is_made_with_correct_body_content()
    {
        SetupValidAuthRequestUsingWhen();

        var addMovieRequest = new AddMovieRequest("My Movie", 2023);

        var addRequest = _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .WithJsonContent<AddMovieRequest>(movie => movie.Year == 2023)
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        await _target.AddAsync(addMovieRequest);

        _mockHttp
            .GetMatchCount(addRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Add_movie_request_returns_movie_id()
    {
        SetupValidAuthRequestUsingWhen();

        var movie = new Movie(42, "My Movie", 2023);

        _mockHttp
            .When(HttpMethod.Post, "/api/movies")
            .Respond(HttpStatusCode.OK, "application/json", "{}");
        
        var result = await _target.AddAsync(new("My Movie", 2023));

        result.Should().Be(movie.Id);
    }
    
    [Fact]
    public async Task Search_movie_request_is_made_with_no_parameters()
    {
        SetupValidAuthRequestUsingWhen();

        var searchRequest = _mockHttp
            .When(HttpMethod.Get, "/api/movies")
            .WithQueryString("")
            .Respond(HttpStatusCode.OK, "application/json", "[]");
        
        await _target.SearchAsync(new(null, null));

        _mockHttp
            .GetMatchCount(searchRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Search_movie_request_is_made_with_title_parameter()
    {
        SetupValidAuthRequestUsingWhen();

        var searchRequest = _mockHttp
            .When(HttpMethod.Get, "/api/movies")
            .WithExactQueryString(new Dictionary<string, string>
            {
                { "title", "My Movie" }
            })
            .Respond(HttpStatusCode.OK, "application/json", "[]");
        
        await _target.SearchAsync(new("My Movie", null));

        _mockHttp
            .GetMatchCount(searchRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Search_movie_request_is_made_with_year_parameter()
    {
        SetupValidAuthRequestUsingWhen();

        var searchRequest = _mockHttp
            .When(HttpMethod.Get, "/api/movies")
            .WithExactQueryString(new Dictionary<string, string>
            {
                // { "year", "2023" }
            })
            .Respond(HttpStatusCode.OK, "application/json", "[]");
        
        await _target.SearchAsync(new(null, 2023));

        _mockHttp
            .GetMatchCount(searchRequest)
            .Should()
            .Be(1);
    }
    
    [Fact]
    public async Task Search_movie_request_is_made_with_title_and_year_parameters()
    {
        SetupValidAuthRequestUsingWhen();

        var searchRequest = _mockHttp
            .When(HttpMethod.Get, "/api/movies")
            .WithExactQueryString(new Dictionary<string, string>
            {
                { "title", "My Movie" },
                { "year", "2023" }
            })
            .Respond(HttpStatusCode.OK, "application/json", "[]");
        
        await _target.SearchAsync(new("My Movie", 2023));

        _mockHttp
            .GetMatchCount(searchRequest)
            .Should()
            .Be(1);
    }

    private void SetupValidAuthRequestUsingWhen()
    {
        var expiry = DateTime.UtcNow.AddMinutes(30);
        var tokenResponse = new TokenResponse("my-token", expiry);
        
        _mockHttp
            .When("/auth/token")
            .RespondWithJsonObject(tokenResponse);
    }
    
    private void SetupValidAuthRequestUsingExpect()
    {
        var expiry = DateTime.UtcNow.AddMinutes(30);
        var tokenResponse = new TokenResponse("my-token", expiry);
        
        _mockHttp
            .Expect("/auth/token")
            .RespondWithJsonObject(tokenResponse);
    }
    
    public void Dispose()
    {
        _mockHttp.Dispose();
        _httpClient.Dispose();
    }
}