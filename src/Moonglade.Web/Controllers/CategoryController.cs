using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Category;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class CategoryController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<CategoryController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([Range(1, int.MaxValue)] int id)
    {
        var cat = await queryMediator.QueryAsync(new GetCategoryQuery(id));
        if (cat == null) return NotFound();

        return Ok(cat);
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var list = await queryMediator.QueryAsync(new ListCategoriesQuery());
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        await CommandMediator.SendAsync(command);

        await LogActivityAsync(
            ActivityEvents.CategoryCreated,
            command.DisplayName,
            ActivityLogDetails.Create(("Slug", command.Slug)));

        return Created(string.Empty, command);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([Range(1, int.MaxValue)] int id, UpdateCategoryCommand command)
    {
        command.Id = id;
        var oc = await CommandMediator.SendAsync(command);
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        await LogActivityAsync(
            ActivityEvents.CategoryUpdated,
            command.DisplayName,
            ActivityLogDetails.Create(
                ("Id", command.Id),
                ("Slug", command.Slug)),
            targetId: id.ToString(CultureInfo.InvariantCulture));

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await CommandMediator.SendAsync(new DeleteCategoryCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        await LogActivityAsync(
            ActivityEvents.CategoryDeleted,
            id.ToString(CultureInfo.InvariantCulture),
            ActivityLogDetails.Create(("CategoryId", id)),
            targetId: id.ToString(CultureInfo.InvariantCulture));

        return NoContent();
    }
}
