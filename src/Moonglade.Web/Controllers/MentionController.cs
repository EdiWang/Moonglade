using Edi.AspNetCore.Utils;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Email.Client;
using Moonglade.Mention.Common;
using Moonglade.Web.Attributes;
using Moonglade.Webmention;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MentionController(
    ILogger<MentionController> logger,
    IBlogConfig blogConfig,
    IEventMediator eventMediator,
    ICommandMediator commandMediator) : ControllerBase
{
    [HttpPost("/webmention")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveWebmention(
        [FromForm][Required] string source,
        [FromForm][Required] string target)
    {
        if (!blogConfig.AdvancedSettings.EnableWebmention) return Forbid();

        var ip = ClientIPHelper.GetClientIP(HttpContext);
        var response = await commandMediator.SendAsync(new ReceiveWebmentionCommand(source, target, ip));

        if (response.Status == WebmentionStatus.Success)
        {
            SendMentionEmailAction(response.MentionEntity);
            return Ok("Webmention received and verified.");
        }

        return HandleWebmentionFailure(response.Status);
    }

    private ObjectResult HandleWebmentionFailure(WebmentionStatus status) => status switch
    {
        WebmentionStatus.InvalidWebmentionRequest => BadRequest("Invalid webmention request."),
        WebmentionStatus.ErrorSourceNotContainTargetUri => Conflict("The source URI does not contain a link to the target URI."),
        WebmentionStatus.SpamDetectedFakeNotFound => NotFound("The requested resource was not found."),
        WebmentionStatus.ErrorTargetUriNotExist => Conflict("Cannot retrieve post ID and title for the target URL."),
        WebmentionStatus.ErrorWebmentionAlreadyRegistered => Conflict("Webmention already registered."),
        WebmentionStatus.GenericError => StatusCode(StatusCodes.Status500InternalServerError, "An internal server error occurred."),
        _ => StatusCode(StatusCodes.Status500InternalServerError, "An unknown error occurred.")
    };

    private async void SendMentionEmailAction(MentionEntity mention)
    {
        try
        {
            await eventMediator.PublishAsync(new MentionEvent(mention.TargetPostTitle, mention.Domain, mention.SourceIp, mention.SourceUrl, mention.SourceTitle));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception occurred while publishing MentionEvent: {Message}", e.Message);
        }
    }

    [Authorize]
    [HttpDelete("{mentionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid mentionId)
    {
        await commandMediator.SendAsync(new DeleteMentionCommand(mentionId));
        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear()
    {
        await commandMediator.SendAsync(new ClearMentionsCommand());
        return NoContent();
    }
}