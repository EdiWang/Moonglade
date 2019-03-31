using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    public class ErrorController : Controller
    {
        protected readonly ILogger<ErrorController> Logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            if (null != logger) Logger = logger;
        }

        [Route("/error")]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            if (statusCode.HasValue)
            {
                this.HttpContext.Response.StatusCode = statusCode.Value;
                return File($"~/errorpages/{statusCode}.html", "text/html");
            }

            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature != null)
            {
                // Get which route the exception occurred at
                var routeWhereExceptionOccurred = exceptionFeature.Path;

                // Get the exception that occurred
                var exceptionThatOccurred = exceptionFeature.Error;
                Logger.LogError($"Shit happens: {routeWhereExceptionOccurred}, client IP: {HttpContext.Connection.RemoteIpAddress}", exceptionThatOccurred);
            }
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}