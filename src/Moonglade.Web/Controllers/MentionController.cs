using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.BackgroundServices;
using Moonglade.Data.Entities;
using Moonglade.Email.Client;
using Moonglade.Webmention;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MentionController(
    ILogger<MentionController> logger,
    IBlogConfig blogConfig,
    IQueryMediator queryMediator,
    CannonService cannonService,
    ICommandMediator commandMediator) : ControllerBase
{
    [HttpPost("/webmention")]
    [IgnoreAntiforgeryToken]
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

    private void SendMentionEmailAction(MentionEntity mention)
    {
        cannonService.FireAsync<IEventMediator>(async mediator =>
            await mediator.PublishAsync(new MentionEvent(
                mention.TargetPostTitle,
                mention.Domain,
                mention.SourceIp,
                mention.SourceUrl,
                mention.SourceTitle)));
    }

    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> ListMentions(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string domain = null,
        [FromQuery] string sourceTitle = null,
        [FromQuery] string targetPostTitle = null,
        [FromQuery] DateTime? startTimeUtc = null,
        [FromQuery] DateTime? endTimeUtc = null,
        [FromQuery] string sortBy = null,
        [FromQuery] bool sortDescending = true)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (mentions, totalCount) = await queryMediator.QueryAsync(
            new ListMentionsQuery(pageSize, pageIndex, domain, sourceTitle, targetPostTitle, startTimeUtc, endTimeUtc, sortBy, sortDescending));

        return Ok(new PagedResult<MentionEntity>(mentions, pageIndex, pageSize, totalCount));
    }

    [Authorize]
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] List<Guid> mentionIds)
    {
        if (mentionIds == null || mentionIds.Count == 0)
        {
            return BadRequest("No mention IDs provided.");
        }

        await commandMediator.SendAsync(new DeleteMentionsCommand(mentionIds));
        return NoContent();
    }

    [Authorize]
    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        await commandMediator.SendAsync(new ClearMentionsCommand());
        return NoContent();
    }
}