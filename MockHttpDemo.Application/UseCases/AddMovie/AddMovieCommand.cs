namespace MockHttpDemo.Application.UseCases.AddMovie;

public record AddMovieCommand(string Title, int Year) : ICommand<int>;