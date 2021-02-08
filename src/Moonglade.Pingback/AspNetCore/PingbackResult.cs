using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback.AspNetCore
{
    public class PingbackResult : IActionResult
    {
        public PingbackResponse PingbackResponse { get; }

        private ILogger<PingbackResult> _logger;

        public PingbackResult(PingbackResponse pingbackResponse)
        {
            PingbackResponse = pingbackResponse;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PingbackResult>>();
            _logger.LogInformation($"Executing PingbackResult for '{PingbackResponse}'");

            string content = null;
            int statusCode = StatusCodes.Status201Created;
            IActionResult actionResult = null;

            switch (PingbackResponse)
            {
                case PingbackResponse.Success:
                    content =
                        "<methodResponse><params><param><value><string>Thanks!</string></value></param></params></methodResponse>";
                    break;
                case PingbackResponse.Error17SourceNotContainTargetUri:
                    content = BuildErrorResponseBody(17,
                        "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                    break;
                case PingbackResponse.Error32TargetUriNotExist:
                    content = BuildErrorResponseBody(32, "The specified target URI does not exist.");
                    break;
                case PingbackResponse.Error48PingbackAlreadyRegistered:
                    content = BuildErrorResponseBody(48, "The pingback has already been registered.");
                    break;
                case PingbackResponse.SpamDetectedFakeNotFound:
                    actionResult = new NotFoundResult();
                    break;
                case PingbackResponse.GenericError:
                    actionResult = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    break;
                case PingbackResponse.InvalidPingRequest:
                    actionResult = new BadRequestResult();
                    break;
                default:
                    _logger.LogError($"Error Executing PingbackResult, invalid PingbackResponse '{PingbackResponse}'");
                    throw new ArgumentOutOfRangeException();
            }

            actionResult ??= new ContentResult
            {
                Content = content,
                ContentType = "text/xml",
                StatusCode = statusCode
            };

            return actionResult.ExecuteResultAsync(context);
        }

        private static string BuildErrorResponseBody(int code, string message)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\"?>");
            sb.Append("<methodResponse>");
            sb.Append("<fault>");
            sb.Append("<value>");
            sb.Append("<struct>");
            sb.Append("<member>");
            sb.Append("<name>faultCode</name>");
            sb.AppendFormat("<value><int>{0}</int></value>", code);
            sb.Append("</member>");
            sb.Append("<member>");
            sb.Append("<name>faultString</name>");
            sb.AppendFormat("<value><string>{0}</string></value>", message);
            sb.Append("</member>");
            sb.Append("</struct>");
            sb.Append("</value>");
            sb.Append("</fault>");
            sb.Append("</methodResponse>");

            return sb.ToString();
        }
    }
}
