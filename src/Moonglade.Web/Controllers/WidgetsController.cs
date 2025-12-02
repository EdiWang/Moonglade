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
    [HttpGet("{id:guid}")]
    [ProducesResponseType<WidgetEntity>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var widget = await queryMediator.QueryAsync(new GetWidgetQuery(id));
        if (null == widget) return NotFound();

        return Ok(widget);
    }
}
