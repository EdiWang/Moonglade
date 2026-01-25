using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Moonglade.IndexNow.Client;

public class IndexNowMapHandler
{
    public static Delegate Handler => Handle;

    public static IResult Handle(IConfiguration configuration)
    {
        var apiKey = configuration["IndexNow:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.NotFound("No IndexNow API Key is present.");
        }

        return Results.Text(apiKey, "text/plain");
    }
}