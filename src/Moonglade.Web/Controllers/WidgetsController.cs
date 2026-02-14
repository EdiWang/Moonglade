using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Widgets;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class WidgetsController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    #region Widget CRUD Operations

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var widget = await queryMediator.QueryAsync(new GetWidgetQuery(id));
        if (null == widget) return NotFound();

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
        await CommandMediator.SendAsync(new CreateWidgetCommand(request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        // Log activity
        await LogActivityAsync(
            EventType.WidgetCreated,
            "Create Widget",
            request.Title,
            new { request.WidgetType });

        return Created();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditWidgetRequest request)
    {
        await CommandMediator.SendAsync(new UpdateWidgetCommand(id, request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        // Log activity
        await LogActivityAsync(
            EventType.WidgetUpdated,
            "Update Widget",
            request.Title,
            new { WidgetId = id, request.WidgetType });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await CommandMediator.SendAsync(new DeleteWidgetCommand(id));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        // Log activity
        await LogActivityAsync(
            EventType.WidgetDeleted,
            "Delete Widget",
            $"Widget #{id}",
            new { WidgetId = id });

        return NoContent();
    }

    #endregion
}
