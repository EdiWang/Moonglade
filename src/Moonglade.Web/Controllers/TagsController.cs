using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Features.Tag;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TagsController(IQueryMediator queryMediator, ICommandMediator commandMediator) : ControllerBase
{
    [HttpGet("names")]
    [ProducesResponseType<List<string>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Names()
    {
        var names = await queryMediator.QueryAsync(new ListTagNamesQuery());
        return Ok(names);
    }

    [HttpGet("list")]
    [ProducesResponseType<List<TagEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new ListTagsQuery());
        return Ok(list);
    }

    [HttpGet("list/count")]
    [ProducesResponseType<List<TagWithCount>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TagCountList()
    {
        var list = await queryMediator.QueryAsync(new GetTagCountListQuery());
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([Required][FromBody] string name)
    {
        if (!BlogTagHelper.IsValidTagName(name)) return Problem(detail: "Invalid tag name.", statusCode: StatusCodes.Status409Conflict);

        var tag = await commandMediator.SendAsync(new CreateTagCommand(name.Trim()));
        return Ok(tag);
    }

    [HttpPut("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [Required][FromBody] string name)
    {
        var oc = await commandMediator.SendAsync(new UpdateTagCommand(id, name));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([Range(0, int.MaxValue)] int id)
    {
        var oc = await commandMediator.SendAsync(new DeleteTagCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}