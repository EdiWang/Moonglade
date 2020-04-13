using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback.Mvc
{
    public class PingbackResult : IActionResult
    {
        public PingbackServiceResponse PingbackServiceResponse { get; }

        private ILogger<PingbackResult> _logger;

        public PingbackResult(PingbackServiceResponse pingbackServiceResponse)
        {
            PingbackServiceResponse = pingbackServiceResponse;
        }

        public Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<PingbackResult>>();
            _logger.LogInformation($"Executing PingbackResult for '{PingbackServiceResponse}'");

            string content = null;
            int statusCode = StatusCodes.Status200OK;
            IActionResult actionResult = null;

            switch (PingbackServiceResponse)
            {
                case PingbackServiceResponse.Success:
                    content =
                        "<methodResponse><params><param><value><string>Thanks!</string></value></param></params></methodResponse>";
                    break;
                case PingbackServiceResponse.Error17SourceNotContainTargetUri:
                    content = BuildErrorResponseBody(17,
                        "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                    break;
                case PingbackServiceResponse.Error32TargetUriNotExist:
                    content = BuildErrorResponseBody(32, "The specified target URI does not exist.");
                    break;
                case PingbackServiceResponse.Error48PingbackAlreadyRegistered:
                    content = BuildErrorResponseBody(48, "The pingback has already been registered.");
                    break;
                case PingbackServiceResponse.SpamDetectedFakeNotFound:
                    actionResult = new NotFoundResult();
                    break;
                case PingbackServiceResponse.GenericError:
                    actionResult = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                    break;
                case PingbackServiceResponse.InvalidPingRequest:
                    actionResult = new BadRequestResult();
                    break;
                default:
                    _logger.LogError($"Error Executing PingbackResult, invalid PingbackServiceResponse '{PingbackServiceResponse}'");
                    throw new ArgumentOutOfRangeException();
            }

            if (null == actionResult)
            {
                actionResult = new ContentResult
                {
                    Content = content,
                    ContentType = "text/xml",
                    StatusCode = statusCode
                };
            }

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
