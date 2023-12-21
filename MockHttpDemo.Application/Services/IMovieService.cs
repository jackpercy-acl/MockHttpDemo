using MockHttpDemo.Application.Dtos;

namespace MockHttpDemo.Application.Services;

public interface IMovieService
{
    Task<ICollection<Movie>> SearchAsync(SearchMoviesRequest request);

    Task<int> AddAsync(AddMovieRequest request);
}