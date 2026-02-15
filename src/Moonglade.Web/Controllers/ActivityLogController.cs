using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActivityLogController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] int[]? eventTypes = null,
        [FromQuery] DateTime? startTimeUtc = null,
        [FromQuery] DateTime? endTimeUtc = null)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        EventType[]? eventTypeEnums = eventTypes?.Select(et => (EventType)et).ToArray();

        var (logs, totalCount) = await queryMediator.QueryAsync(
            new ListActivityLogsQuery(pageSize, pageIndex, eventTypeEnums, startTimeUtc, endTimeUtc));

        return Ok(new
        {
            Logs = logs,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

    [HttpGet("{id:long}/metadata")]
    public async Task<IActionResult> GetMetadata([NotEmpty] long id)
    {
        var metadata = await queryMediator.QueryAsync(new GetMetaDataByActivityLogIdQuery(id));

        if (metadata == null) return NotFound();

        return Ok(metadata);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete([NotEmpty] long id)
    {
        var oc = await CommandMediator.SendAsync(new DeleteActivityLogCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        await LogActivityAsync(
            EventType.ActivityLogDeleted,
            "Delete Activity Log",
            $"Activity Log #{id}",
            new { ActivityLogId = id });

        return NoContent();
    }
}
