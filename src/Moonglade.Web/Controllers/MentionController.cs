using Moonglade.Data.Entities;
using Moonglade.Email.Client;
using Moonglade.Mention.Common;
using Moonglade.Pingback;
using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;
using Moonglade.Webmention;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MentionController(
    ILogger<MentionController> logger,
    IBlogConfig blogConfig,
    IMediator mediator) : ControllerBase
{
    [HttpPost("/webmention")]
    public async Task<IActionResult> ReceiveWebmention(
        [FromForm][Required] string source,
        [FromForm][Required] string target)
    {
        if (!blogConfig.AdvancedSettings.EnableWebmention) return Forbid();

        var ip = Helper.GetClientIP(HttpContext);
        var response = await mediator.Send(new ReceiveWebmentionCommand(source, target, ip));
        if (response.Status == WebmentionStatus.Success)
        {
            SendMentionEmailAction(response.MentionEntity);
            return Ok("Webmention received and verified.");
        }

        // TODO
        return BadRequest("Webmention verification failed.");
    }

    [HttpPost("/pingback")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ReceivePingback()
    {
        if (!blogConfig.AdvancedSettings.EnablePingback) return Forbid();

        var ip = Helper.GetClientIP(HttpContext);
        var requestBody = await new StreamReader(HttpContext.Request.Body, Encoding.Default).ReadToEndAsync();

        var response = await mediator.Send(new ReceivePingCommand(requestBody, ip));
        if (response.Status == PingbackStatus.Success)
        {
            SendMentionEmailAction(response.MentionEntity);
        }

        return new PingbackResult(response.Status);
    }

    private async void SendMentionEmailAction(MentionEntity history)
    {
        try
        {
            await mediator.Publish(new MentionNotification(history.TargetPostTitle, history.Domain, history.SourceIp, history.SourceUrl, history.SourceTitle));
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