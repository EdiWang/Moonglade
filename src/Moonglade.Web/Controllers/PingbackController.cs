using System;
using System.Text;
using System.Threading.Tasks;
using Edi.Blog.Pingback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("pingback")]
    public class PingbackController : MoongladeController
    {
        private readonly PingbackService _pingbackService;

        public PingbackController(
            ILogger<PingbackController> logger,
            IOptions<AppSettings> settings,
            PingbackService pingbackService)
            : base(logger, settings)
        {
            _pingbackService = pingbackService;
        }

        [HttpPost("")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index()
        {
            if (!AppSettings.EnablePingBackReceive)
            {
                Logger.LogInformation("Pingback receive is disabled");
                return Forbid();
            }

            var response = await _pingbackService.ProcessReceivedPingback(HttpContext);
            Logger.LogInformation($"Pingback Processor Response: {response.ToString()}");
            switch (response)
            {
                case PingbackServiceResponse.Success:
                    return Content(PingbackReceiver.SuccessResponseString, "text/xml");
                case PingbackServiceResponse.Error17SourceNotContainTargetUri:
                    return XmlContent(17, "The source URI does not contain a link to the target URI, and so cannot be used as a source.");
                case PingbackServiceResponse.Error32TargetUriNotExist:
                    return XmlContent(32, "The specified target URI does not exist.");
                case PingbackServiceResponse.Error48PingbackAlreadyRegistered:
                    return XmlContent(48, "The pingback has already been registered.");
                case PingbackServiceResponse.SpamDetectedFakeNotFound:
                    return NotFound();
                case PingbackServiceResponse.GenericError:
                    return ServerError();
                default:
                    return BadRequest();
            }
        }

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var data = await _pingbackService.GetReceivedPingbacksAsync();
            return View(data);
        }

        [Authorize]
        [HttpPost("delete")]
        public IActionResult Delete(Guid pingbackId)
        {
            return _pingbackService.DeleteReceivedPingback(pingbackId).IsSuccess ? Json(pingbackId) : Json(null);
        }

        private ContentResult XmlContent(int code, string message)
        {
            return Content(BuildErrorResponseBody(code, message), "text/xml");
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