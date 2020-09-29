using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moonglade.Pingback;
using Moonglade.Pingback.Mvc;

namespace Moonglade.Web.Controllers
{
    [Route("pingback")]
    public class PingbackController : BlogController
    {
        private readonly LegacyPingbackService _legacyPingbackService;
        private readonly IBlogConfig _blogConfig;
        private readonly IPingbackService _pingbackService;

        public PingbackController(
            ILogger<PingbackController> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            LegacyPingbackService legacyPingbackService,
            IPingbackService pingbackService)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _legacyPingbackService = legacyPingbackService;
            _pingbackService = pingbackService;
        }

        [HttpPost("")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index()
        {
            if (!_blogConfig.AdvancedSettings.EnablePingBackReceive)
            {
                Logger.LogInformation("Pingback receive is disabled");
                return Forbid();
            }

            var response = await _legacyPingbackService.ProcessReceivedPayloadAsync(HttpContext);
            Logger.LogInformation($"Pingback Processor Response: {response}");
            return new PingbackResult(response);
        }

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var list = await _pingbackService.GetPingbackHistoryAsync();
            return View("~/Views/Admin/ManagePingback.cshtml", list);
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid pingbackId, [FromServices] IBlogAudit blogAudit)
        {
            if (_legacyPingbackService.DeleteReceivedPingback(pingbackId).IsSuccess)
            {
                await blogAudit.AddAuditEntry(EventType.Content, AuditEventId.PingbackDeleted,
                    $"Pingback '{pingbackId}' deleted.");
                return Json(pingbackId);
            }
            return Json(null);
        }
    }
}