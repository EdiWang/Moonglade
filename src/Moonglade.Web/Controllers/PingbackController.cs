using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Web.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Web.Controllers
{
    [ApiController]
    [Route("pingback")]
    public class PingbackController : ControllerBase
    {
        private readonly ILogger<PingbackController> _logger;
        private readonly IBlogConfig _blogConfig;
        private readonly IPingbackService _pingbackService;
        private readonly IMediator _mediator;
        private readonly IBlogNotificationClient _notificationClient;

        public PingbackController(
            ILogger<PingbackController> logger,
            IBlogConfig blogConfig,
            IPingbackService pingbackService,
            IMediator mediator,
            IBlogNotificationClient notificationClient)
        {
            _logger = logger;
            _blogConfig = blogConfig;
            _pingbackService = pingbackService;
            _mediator = mediator;
            _notificationClient = notificationClient;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete([NotEmpty] Guid pingbackId, [FromServices] IBlogAudit blogAudit)
        {
            await _mediator.Send(new DeletePingbackCommand(pingbackId));
            await blogAudit.AddEntry(BlogEventType.Content, BlogEventId.PingbackDeleted,
                $"Pingback '{pingbackId}' deleted.");
            return NoContent();
        }
    }
}