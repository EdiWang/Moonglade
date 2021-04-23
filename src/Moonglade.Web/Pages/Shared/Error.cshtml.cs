using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models;

namespace Moonglade.Web.Pages.Shared
{
    public class ErrorModel : PageModel
    {
        private readonly ILogger<ErrorModel> _logger;

        public ErrorViewModel ErrorViewModel { get; set; }

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
            ErrorViewModel = new();
        }

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
                _logger.LogError($"Error: {routeWhereExceptionOccurred}, " +
                                 $"client IP: {HttpContext.Connection.RemoteIpAddress}, " +
                                 $"request id: {requestId}", exceptionThatOccurred);
            }

            ErrorViewModel.RequestId = requestId;
        }
    }
}
