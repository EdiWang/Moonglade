using Moonglade.Notification.Client;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("pingback")]
public class PingbackController : ControllerBase
{
    private readonly ILogger<PingbackController> _logger;
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;

    public PingbackController(
        ILogger<PingbackController> logger,
        IBlogConfig blogConfig,
        IMediator mediator)
    {
        _logger = logger;
        _blogConfig = blogConfig;
        _mediator = mediator;
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Process([FromServices] IServiceScopeFactory factory)
    {
        if (!_blogConfig.AdvancedSettings.EnablePingbackReceive)
        {
            _logger.LogInformation("Pingback receive is disabled");
            return Forbid();
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var requestBody = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();

        var response = await _mediator.Send(new ReceivePingCommand(requestBody, ip,
            history =>
            {
                _ = Task.Run(async () =>
                {
                    var scope = factory.CreateScope();
                    var mediator = scope.ServiceProvider.GetService<IMediator>();
                    if (mediator != null)
                    {
                        await mediator.Publish(new PingbackNotification(
                            history.TargetPostTitle,
                            history.PingTimeUtc,
                            history.Domain,
                            history.SourceIp,
                            history.SourceUrl,
                            history.SourceTitle));
                    }
                });
            }));

        _logger.LogInformation($"Pingback Processor Response: {response}");
        return new PingbackResult(response);
    }

    [Authorize]
    [HttpDelete("{pingbackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid pingbackId)
    {
        await _mediator.Send(new DeletePingbackCommand(pingbackId));
        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await _mediator.Send(new ClearPingbackCommand());
        return NoContent();
    }
}