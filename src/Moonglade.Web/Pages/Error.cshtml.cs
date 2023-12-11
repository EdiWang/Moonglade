using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;

namespace Moonglade.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel(ILogger<ErrorModel> logger) : PageModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionFeature is not null)
        {
            // Get which route the exception occurred at
            var routeWhereExceptionOccurred = exceptionFeature.Path;

            // Get the exception that occurred
            var exceptionThatOccurred = exceptionFeature.Error;
            logger.LogError($"Error: {routeWhereExceptionOccurred}, " +
                             $"client IP: {Helper.GetClientIP(HttpContext)}, " +
                             $"request id: {requestId}", exceptionThatOccurred);
        }

        RequestId = requestId;
    }
}