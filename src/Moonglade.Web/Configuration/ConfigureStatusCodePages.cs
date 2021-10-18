using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.WebUtilities;

namespace Moonglade.Web.Configuration
{
    public class ConfigureStatusCodePages
    {
        public static Func<StatusCodeContext, Task> Handler => async context =>
        {
            var statusCode = context.HttpContext.Response.StatusCode;
            var requestId = context.HttpContext.TraceIdentifier;
            var description = ReasonPhrases.GetReasonPhrase(context.HttpContext.Response.StatusCode);

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                statusCode,
                requestId,
                description
            }, context.HttpContext.RequestAborted);
        };
    }
}
