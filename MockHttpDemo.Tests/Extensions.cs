using System.Text.Json;
using RichardSzalay.MockHttp;

namespace MockHttpDemo.Tests;

public static class Extensions
{
    public static MockedRequest RespondWithJsonObject<T>(this MockedRequest request, T responseObject)
    {
        var json = JsonSerializer.Serialize(responseObject);

        request.Respond("application/json", json);

        return request;
    }
}