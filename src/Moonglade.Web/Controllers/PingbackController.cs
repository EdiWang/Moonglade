using Moonglade.Data.Entities;
using Moonglade.Email.Client;
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
    public async Task<IActionResult> Process()
    {
        if (!_blogConfig.AdvancedSettings.EnablePingback) return Forbid();

        var ip = Helper.GetClientIP(HttpContext);
        var requestBody = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();

        var response = await _mediator.Send(new ReceivePingCommand(requestBody, ip, SendPingbackEmailAction));

        return new PingbackResult(response);
    }

    private async void SendPingbackEmailAction(PingbackEntity history)
    {
        try
        {
            await _mediator.Publish(new PingbackNotification(history.TargetPostTitle, history.Domain, history.SourceIp, history.SourceUrl, history.SourceTitle));
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
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