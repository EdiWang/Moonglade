using Moonglade.Data.Entities;
using Moonglade.FriendLink;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FriendLinkController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(EditLinkRequest request)
    {
        await mediator.Send(new AddLinkCommand(request));
        return Created(new Uri(request.LinkUrl), request);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FriendLinkEntity>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var link = await mediator.Send(new GetLinkQuery(id));
        if (null == link) return NotFound();

        return Ok(link);
    }

    [HttpGet("list")]
    [ProducesResponseType<List<FriendLinkEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var list = await mediator.Send(new GetAllLinksQuery());
        return Ok(list);
    }

    [HttpPut("{id:guid}")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditLinkRequest request)
    {
        await mediator.Send(new UpdateLinkCommand(id, request));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await mediator.Send(new DeleteLinkCommand(id));
        return NoContent();
    }
}