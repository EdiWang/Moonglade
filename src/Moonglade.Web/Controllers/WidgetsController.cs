using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Widgets;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class WidgetsController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<WidgetsController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var widget = await queryMediator.QueryAsync(new GetWidgetQuery(id));
        if (widget == null) return NotFound();

        return Ok(widget);
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new ListWidgetsQuery());
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EditWidgetRequest request)
    {
        var id = await CommandMediator.SendAsync(new CreateWidgetCommand(request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        await LogActivityAsync(
            ActivityEvents.WidgetCreated,
            request.Title,
            ActivityLogDetails.Create(("WidgetType", request.WidgetType)),
            targetId: id.ToString());

        return Created();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditWidgetRequest request)
    {
        await CommandMediator.SendAsync(new UpdateWidgetCommand(id, request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        await LogActivityAsync(
            ActivityEvents.WidgetUpdated,
            request.Title,
            ActivityLogDetails.Create(
                ("WidgetId", id),
                ("WidgetType", request.WidgetType)),
            targetId: id.ToString());

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await CommandMediator.SendAsync(new DeleteWidgetCommand(id));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        await LogActivityAsync(
            ActivityEvents.WidgetDeleted,
            $"Widget #{id}",
            ActivityLogDetails.Create(("WidgetId", id)),
            targetId: id.ToString());

        return NoContent();
    }
}
