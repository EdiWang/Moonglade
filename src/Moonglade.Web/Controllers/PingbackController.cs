using Moonglade.Data.Entities;
using Moonglade.Email.Client;
using Moonglade.Mention.Common;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("pingback")]
public class PingbackController(
        ILogger<PingbackController> logger,
        IBlogConfig blogConfig,
        IMediator mediator) : ControllerBase
{
    [HttpPost]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Process()
    {
        if (!blogConfig.AdvancedSettings.EnablePingback) return Forbid();

        var ip = Helper.GetClientIP(HttpContext);
        var requestBody = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();

        var response = await mediator.Send(new ReceivePingCommand(requestBody, ip));
        if (response.Status == PingbackStatus.Success)
        {
            SendPingbackEmailAction(response.MentionEntity);
        }

        return new PingbackResult(response.Status);
    }

    private async void SendPingbackEmailAction(MentionEntity history)
    {
        try
        {
            await mediator.Publish(new PingbackNotification(history.TargetPostTitle, history.Domain, history.SourceIp, history.SourceUrl, history.SourceTitle));
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
    }

    [Authorize]
    [HttpDelete("{pingbackId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid pingbackId)
    {
        await mediator.Send(new DeleteMentionCommand(pingbackId));
        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await mediator.Send(new ClearMentionsCommand());
        return NoContent();
    }
}