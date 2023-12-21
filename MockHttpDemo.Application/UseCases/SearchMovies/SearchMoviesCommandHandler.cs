using MockHttpDemo.Application.Dtos;
using MockHttpDemo.Application.Services;

namespace MockHttpDemo.Application.UseCases.SearchMovies;

public class SearchMoviesCommandHandler : ICommandHandler<SearchMoviesCommand, ICollection<Movie>>
{
    private readonly IMovieService _movieService;

    public SearchMoviesCommandHandler(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public async Task<ICollection<Movie>> HandleAsync(SearchMoviesCommand command)
    {
        var request = new SearchMoviesRequest(command.Title, command.Year);
        
        try
        {
            ICollection<Movie> result = await _movieService.SearchAsync(request);
            return result;
        }
        catch (Exception e)
        {
            throw new ApplicationException("An error occurred searching for movies", e);
        }
    }
}