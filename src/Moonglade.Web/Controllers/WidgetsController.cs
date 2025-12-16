using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Web.Attributes;
using Moonglade.Widgets;

namespace Moonglade.Web.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class WidgetsController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : ControllerBase
{
    #region Widget CRUD Operations

    [HttpGet("{id:guid}")]
    [ProducesResponseType<WidgetEntity>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var widget = await queryMediator.QueryAsync(new GetWidgetQuery(id));
        if (null == widget) return NotFound();

        return Ok(widget);
    }

    [HttpGet("list")]
    [ProducesResponseType<List<WidgetEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new ListWidgetsQuery());
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(EditWidgetRequest request)
    {
        await commandMediator.SendAsync(new CreateWidgetCommand(request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        return Created();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditWidgetRequest request)
    {
        await commandMediator.SendAsync(new UpdateWidgetCommand(id, request));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await commandMediator.SendAsync(new DeleteWidgetCommand(id));
        cache.Remove(BlogCachePartition.General.ToString(), "widgets");

        return NoContent();
    }

    #endregion
}
