using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Tag;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class TagsController(IQueryMediator queryMediator, ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpGet("names")]
    public async Task<IActionResult> Names()
    {
        var names = await queryMediator.QueryAsync(new ListTagNamesQuery());
        return Ok(names);
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new ListTagsQuery());
        return Ok(list);
    }

    [HttpGet("list/count")]
    public async Task<IActionResult> TagCountList()
    {
        var list = await queryMediator.QueryAsync(new GetTagCountListQuery());
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([Required][FromBody] string name)
    {
        if (!BlogTagHelper.IsValidTagName(name)) return Conflict("Invalid tag name.");

        var tag = await CommandMediator.SendAsync(new CreateTagCommand(name.Trim()));

        await LogActivityAsync(
            EventType.TagCreated,
            "Create Tag",
            name.Trim());

        return Ok(tag);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, [Required][FromBody] string name)
    {
        var oc = await CommandMediator.SendAsync(new UpdateTagCommand(id, name));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        await LogActivityAsync(
            EventType.TagUpdated,
            "Update Tag",
            name,
            new { TagId = id });

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([Range(0, int.MaxValue)] int id)
    {
        var oc = await CommandMediator.SendAsync(new DeleteTagCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        await LogActivityAsync(
            EventType.TagDeleted,
            "Delete Tag",
            $"Tag #{id}",
            new { TagId = id });

        return NoContent();
    }
}