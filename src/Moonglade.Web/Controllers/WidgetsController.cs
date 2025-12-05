using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Utils;
using Moonglade.Web.Attributes;
using Moonglade.Widgets;
using Moonglade.Widgets.Types.LinkList;

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
        // TODO: Add caching
        var list = await queryMediator.QueryAsync(new ListWidgetsQuery());
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(EditWidgetRequest request)
    {
        await commandMediator.SendAsync(new CreateWidgetCommand(request));
        return Created();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditWidgetRequest request)
    {
        await commandMediator.SendAsync(new UpdateWidgetCommand(id, request));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await commandMediator.SendAsync(new DeleteWidgetCommand(id));
        return NoContent();
    }

    #endregion

    #region Widget Link Item CRUD Operations

    [HttpPost("linkitem")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLinkItem(EditWidgetLinkItemRequest request)
    {
        var result = await commandMediator.SendAsync(new CreateWidgetLinkItemCommand(request));

        if (result == OperationCode.ObjectNotFound)
        {
            return NotFound($"Widget with ID {request.WidgetId} not found");
        }

        return Created();
    }

    [HttpGet("linkitem/list/{widgetId:guid}")]
    [ProducesResponseType<List<WidgetLinkItemEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLinkItems([NotEmpty] Guid widgetId)
    {
        var list = await queryMediator.QueryAsync(new ListWidgetLinkItemsByWidgetIdQuery(widgetId));
        return Ok(list);
    }

    [HttpPut("linkitem/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLinkItem([NotEmpty] Guid id, EditWidgetLinkItemRequest request)
    {
        var result = await commandMediator.SendAsync(new UpdateWidgetLinkItemCommand(id, request));

        if (result == OperationCode.ObjectNotFound)
        {
            return NotFound($"Widget link item with ID {id} not found");
        }

        return NoContent();
    }

    [HttpDelete("linkitem/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteLinkItem([NotEmpty] Guid id)
    {
        await commandMediator.SendAsync(new DeleteWidgetLinkItemCommand(id));
        return NoContent();
    }

    #endregion
}
