namespace MockHttpDemo.Application.Config;

public record MovieServiceConfig(
    Uri BaseUri,
    string ClientId,
    string ClientSecret);