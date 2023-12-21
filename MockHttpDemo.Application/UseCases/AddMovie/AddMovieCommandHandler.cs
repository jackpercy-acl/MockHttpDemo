using MockHttpDemo.Application.Dtos;
using MockHttpDemo.Application.Services;

namespace MockHttpDemo.Application.UseCases.AddMovie;

public class AddMovieCommandHandler : ICommandHandler<AddMovieCommand, int>
{
    private readonly IMovieService _movieService;

    public AddMovieCommandHandler(IMovieService movieService)
    {
        _movieService = movieService;
    }

    public async Task<int> HandleAsync(AddMovieCommand command)
    {
        ValidateCommand(command);

        var request = new AddMovieRequest(command.Title, command.Year);
        
        try
        {
            int result = await _movieService.AddAsync(request);
            return result;
        }
        catch (Exception e)
        {
            throw new ApplicationException("An error occurred adding the movie", e);
        }
    }

    private static void ValidateCommand(AddMovieCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Title))
        {
            throw new ApplicationException("Movie must have a name");
        }

        if (command.Year <= 0)
        {
            throw new ApplicationException("Movie must have a positive year");
        }
    }
}