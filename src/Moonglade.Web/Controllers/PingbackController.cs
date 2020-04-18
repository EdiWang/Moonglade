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
using Moonglade.Pingback.Mvc;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Web.Controllers
{
    [Route("pingback")]
    public class PingbackController : MoongladeController
    {
        private readonly PingbackService _pingbackService;
        private readonly IBlogConfig _blogConfig;

        public PingbackController(
            ILogger<PingbackController> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            PingbackService pingbackService)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;
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

            var response = await _pingbackService.ProcessReceivedPingbackAsync(HttpContext);
            Logger.LogInformation($"Pingback Processor Response: {response.ToString()}");
            return new PingbackResult(response);
        }

        [Authorize]
        [Route("manage")]
        public async Task<IActionResult> Manage()
        {
            var response = await _pingbackService.GetReceivedPingbacksAsync();
            return response.IsSuccess ? View(response.Item) : ServerError();
        }

        [Authorize]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete(Guid pingbackId, [FromServices] IMoongladeAudit moongladeAudit)
        {
            if (_pingbackService.DeleteReceivedPingback(pingbackId).IsSuccess)
            {
                await moongladeAudit.AddAuditEntry(EventType.Content, EventId.PingbackDeleted,
                    $"Pingback '{pingbackId}' deleted.");
                return Json(pingbackId);
            }
            return Json(null);
        }
    }
}