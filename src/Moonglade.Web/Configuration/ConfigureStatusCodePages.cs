using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Moonglade.Web.Configuration;

public class ConfigureStatusCodePages
{
    public static Func<StatusCodeContext, Task> Handler => async context =>
    {
        var httpContext = context.HttpContext;
        var statusCode = httpContext.Response.StatusCode;

        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = statusCode,
                Title = ReasonPhrases.GetReasonPhrase(statusCode)
            }
        });
    };
}