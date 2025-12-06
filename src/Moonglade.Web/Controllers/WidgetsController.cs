using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Utils;
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

    #region WidgetContent CRUD Operations

    [HttpGet("content/{id:guid}")]
    [ProducesResponseType<WidgetContentEntity>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContent([NotEmpty] Guid id)
    {
        var widgetContent = await queryMediator.QueryAsync(new GetWidgetContentQuery(id));
        if (null == widgetContent) return NotFound();

        return Ok(widgetContent);
    }

    [HttpPost("content")]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateContent(CreateWidgetContentCommand request)
    {
        var id = await commandMediator.SendAsync(request);
        return CreatedAtAction(nameof(GetContent), new { id }, id);
    }

    [HttpPut("content/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContent([NotEmpty] Guid id, UpdateWidgetContentCommand request)
    {
        if (id != request.Id) return BadRequest("ID mismatch");

        await commandMediator.SendAsync(request);
        return NoContent();
    }

    [HttpDelete("content/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteContent([NotEmpty] Guid id)
    {
        await commandMediator.SendAsync(new DeleteWidgetContentCommand { Id = id });
        return NoContent();
    }

    #endregion
}
