using FluentAssertions;
using MockHttpDemo.Application.Dtos;
using MockHttpDemo.Application.Services;
using MockHttpDemo.Application.UseCases.AddMovie;
using Moq;

namespace MockHttpDemo.Tests.UseCases.AddMovie;

public class AddMovieCommandHandlerTests
{
    private readonly AddMovieCommandHandler _target;

    private readonly Mock<IMovieService> _movieServiceMock;

    public AddMovieCommandHandlerTests()
    {
        _movieServiceMock = new Mock<IMovieService>();

        _target = new AddMovieCommandHandler(_movieServiceMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Throws_exception_when_movie_title_is_invalid(string title)
    {
        var command = new AddMovieCommand(title, 2023);

        Func<Task> act = () => _target.HandleAsync(command);

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Movie must have a name");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(-10)]
    [InlineData(0)]
    public async Task Throws_exception_when_movie_year_is_invalid(int year)
    {
        var command = new AddMovieCommand("My Movie", year);

        Func<Task> act = () => _target.HandleAsync(command);

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("Movie must have a positive year");
    }
    
    [Fact]
    public async Task Throws_exception_when_movie_service_fails()
    {
        var command = new AddMovieCommand("My Movie", 2023);

        _movieServiceMock
            .Setup(movie => movie.AddAsync(It.IsAny<AddMovieRequest>()))
            .ThrowsAsync(new Exception());

        Func<Task> act = () => _target.HandleAsync(command);

        await act
            .Should()
            .ThrowAsync<ApplicationException>()
            .WithMessage("An error occurred adding the movie");
    }
    
    [Fact]
    public async Task Successfully_adds_a_new_movie()
    {
        const int movieId = 42;
        
        var command = new AddMovieCommand("My Movie", 2023);

        _movieServiceMock
            .Setup(movie => movie.AddAsync(It.IsAny<AddMovieRequest>()))
            .ReturnsAsync(movieId);

        var result = await _target.HandleAsync(command);

        result.Should().Be(movieId);
    }
}