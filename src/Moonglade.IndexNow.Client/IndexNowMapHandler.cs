using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Moonglade.IndexNow.Client;

public class IndexNowMapHandler
{
    public static Delegate Handler => async (HttpContext httpContext, IConfiguration configuration) =>
    {
        await Handle(httpContext, configuration);
    };

    public static async Task Handle(HttpContext httpContext, IConfiguration configuration)
    {
        var apiKey = configuration["IndexNow:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            await httpContext.Response.WriteAsync("No indexnowkey.txt is present.", httpContext.RequestAborted);
        }
        else
        {
            httpContext.Response.ContentType = "text/plain";
            await httpContext.Response.WriteAsync(apiKey, Encoding.UTF8, httpContext.RequestAborted);
        }
    }
}