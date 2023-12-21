namespace MockHttpDemo.Application.Dtos;

public record TokenResponse(string Token, DateTimeOffset Expiry);