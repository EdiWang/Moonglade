using System;
using System.Text;
using System.Threading.Tasks;
using Edi.Blog.Pingback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Model.Settings;

namespace Moonglade.Web.Controllers
{
    [Route("pingback")]
    public class PingbackController : MoongladeController
    {
        private readonly PingbackService _pingbackService;

        public PingbackController(MoongladeDbContext context,
            ILogger<PingbackController> logger,
            IOptions<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache, PingbackService pingbackService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _pingbackService = pingbackService;
        }

        public async Task<IActionResult> Index()
        {
            if (!AppSettings.EnablePingBackReceive)
            {
                Logger.LogInformation("Pingback receive is disabled");
                return Forbid();
            }

            var response = await _pingbackService.ProcessReceivedPingback(HttpContext);
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

        private ContentResult XmlContent(int code, string message)
        {
            return Content(BuildErrorResponseBody(code, message), "text/xml");
        }

        private string BuildErrorResponseBody(int code, string message)
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

        [Authorize]
        [Route("manage")]
        public IActionResult Manage()
        {
            var data = _pingbackService.GetReceivedPingbacks();
            return View(data);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("delete")]
        public IActionResult Delete(Guid pingbackId)
        {
            return _pingbackService.DeleteReceivedPingback(pingbackId).IsSuccess ? Json(pingbackId) : Json(null);
        }
    }
}