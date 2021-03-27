using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : Controller
    {
        protected readonly ILogger<ErrorController> Logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            if (logger is not null) Logger = logger;
        }

        [HttpGet("/error")]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature is not null)
            {
                // Get which route the exception occurred at
                var routeWhereExceptionOccurred = exceptionFeature.Path;

                // Get the exception that occurred
                var exceptionThatOccurred = exceptionFeature.Error;
                Logger.LogError($"Error: {routeWhereExceptionOccurred}, " +
                                $"client IP: {HttpContext.Connection.RemoteIpAddress}, " +
                                $"request id: {requestId}", exceptionThatOccurred);
            }
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}