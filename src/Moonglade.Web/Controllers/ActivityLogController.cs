using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActivityLogController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : ControllerBase
{
    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // 限制最大页面大小

        var (logs, totalCount) = await queryMediator.QueryAsync(new ListActivityLogsQuery(pageSize, pageIndex));

        return Ok(new
        {
            Logs = logs,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        });
    }

    [HttpDelete("{id:long}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] long id)
    {
        var oc = await commandMediator.SendAsync(new DeleteActivityLogCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}
