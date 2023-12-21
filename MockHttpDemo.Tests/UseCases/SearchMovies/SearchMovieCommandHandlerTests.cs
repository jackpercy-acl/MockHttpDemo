using FluentAssertions;
using MockHttpDemo.Application.Dtos;
using MockHttpDemo.Application.Services;
using MockHttpDemo.Application.UseCases.SearchMovies;
using Moq;

namespace MockHttpDemo.Tests.UseCases.SearchMovies;

public class SearchMovieCommandHandlerTests
{
    private readonly SearchMoviesCommandHandler _target;

    private readonly Mock<IMovieService> _movieServiceMock;

    public SearchMovieCommandHandlerTests()
    {
        _movieServiceMock = new Mock<IMovieService>();

        _target = new SearchMoviesCommandHandler(_movieServiceMock.Object);
    }
    
    [Fact]
    public async Task Throws_exception_when_movie_service_fails()
    {
        var command = new SearchMoviesCommand(null, null);

        _movieServiceMock
            .Setup(movie => movie.SearchAsync(It.IsAny<SearchMoviesRequest>()))
            .ThrowsAsync(new Exception());

        Func<Task> act = () => _target.HandleAsync(command);

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred searching for movies");
    }
    
    [Theory]
    [InlineData(null, null)]
    [InlineData("My Movie", null)]
    [InlineData(null, 2023)]
    [InlineData("My Movie", 2023)]
    public async Task Successfully_searches_for_movies(string? title, int? year)
    {
        var command = new SearchMoviesCommand(title, year);

        _movieServiceMock
            .Setup(movie => movie.SearchAsync(It.IsAny<SearchMoviesRequest>()))
            .ReturnsAsync(new List<Movie>
            {
                new(1, "My Movie", 2023)
            });

        var result = await _target.HandleAsync(command);

        result.Should().HaveCount(1);
    }
}