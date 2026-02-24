using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Moonglade.Web.Configuration;

public class ConfigureStatusCodePages
{
    public static Func<StatusCodeContext, Task> Handler => async context =>
    {
        var httpContext = context.HttpContext;
        var statusCode = httpContext.Response.StatusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = ReasonPhrases.GetReasonPhrase(statusCode),
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails);
    };
}