using MockHttpDemo.Application.Dtos;

namespace MockHttpDemo.Application.UseCases.SearchMovies;

public record SearchMoviesCommand(string? Title, int? Year) : ICommand<ICollection<Movie>>;