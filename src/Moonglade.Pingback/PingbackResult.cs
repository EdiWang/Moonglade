using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Moonglade.Pingback;

public class PingbackResult(PingbackStatus pingbackStatus) : IActionResult
{
    public PingbackStatus PingbackStatus { get; } = pingbackStatus;

    public Task ExecuteResultAsync(ActionContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        string content = null;
        int statusCode = StatusCodes.Status201Created;
        IActionResult actionResult = null;

        switch (PingbackStatus)
        {
            case PingbackStatus.Success:
                content =
                    "<methodResponse><params><param><value><string>Thanks!</string></value></param></params></methodResponse>";
                break;
            case PingbackStatus.Error17SourceNotContainTargetUri:
                content = BuildErrorResponseBody(17,
                    "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                break;
            case PingbackStatus.Error32TargetUriNotExist:
                content = BuildErrorResponseBody(32, "The specified target URI does not exist.");
                break;
            case PingbackStatus.Error48PingbackAlreadyRegistered:
                content = BuildErrorResponseBody(48, "The pingback has already been registered.");
                break;
            case PingbackStatus.SpamDetectedFakeNotFound:
                actionResult = new NotFoundResult();
                break;
            case PingbackStatus.GenericError:
                actionResult = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                break;
            case PingbackStatus.InvalidPingRequest:
                actionResult = new BadRequestResult();
                break;
            default:
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