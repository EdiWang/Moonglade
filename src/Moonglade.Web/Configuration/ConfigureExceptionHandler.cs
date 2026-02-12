using Microsoft.AspNetCore.Diagnostics;

namespace Moonglade.Web.Configuration;

public static class ConfigureExceptionHandler
{
    public static void Handler(IApplicationBuilder exceptionApp)
    {
        exceptionApp.Run(async context =>
        {
            var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature is not null)
            {
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ExceptionHandler");
                var requestId = context.TraceIdentifier;
                logger.LogError(exceptionFeature.Error,
                    "Unhandled exception at {Path}, client IP: {ClientIP}, request id: {RequestId}",
                    exceptionFeature.Path,
                    ClientIPHelper.GetClientIP(context),
                    requestId);
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
                }
            });
        });
    }
}
