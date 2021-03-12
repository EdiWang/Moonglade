using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Pingback.AspNetCore;
using Moonglade.Web.Filters;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [AppendAppVersion]
    [Route("pingback")]
    public class PingbackController : ControllerBase
    {
        private readonly ILogger<PingbackController> _logger;
        private readonly IBlogConfig _blogConfig;
        private readonly IPingbackService _pingbackService;
        private readonly IBlogNotificationClient _notificationClient;

        public PingbackController(
            ILogger<PingbackController> logger,
            IBlogConfig blogConfig,
            IPingbackService pingbackService,
            IBlogNotificationClient notificationClient)
        {
            _logger = logger;
            _blogConfig = blogConfig;
            _pingbackService = pingbackService;
            _notificationClient = notificationClient;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Process()
        {
            if (!_blogConfig.AdvancedSettings.EnablePingBackReceive)
            {
                _logger.LogInformation("Pingback receive is disabled");
                return Forbid();
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var requestBody = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();

            var response = await _pingbackService.ReceivePingAsync(requestBody, ip,
                history =>
                {
                    _notificationClient.NotifyPingbackAsync(history.TargetPostTitle,
                        history.PingTimeUtc,
                        history.Domain,
                        history.SourceIp,
                        history.SourceUrl,
                        history.SourceTitle);
                });

            _logger.LogInformation($"Pingback Processor Response: {response}");
            return new PingbackResult(response);
        }

        [Authorize]
        [HttpDelete("{pingbackId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid pingbackId, [FromServices] IBlogAudit blogAudit)
        {
            await _pingbackService.DeletePingbackHistory(pingbackId);
            await blogAudit.AddAuditEntry(EventType.Content, AuditEventId.PingbackDeleted,
                $"Pingback '{pingbackId}' deleted.");
            return Ok();
        }
    }
}