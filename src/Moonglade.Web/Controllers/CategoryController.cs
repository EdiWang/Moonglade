using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Features.Category;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class CategoryController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var cat = await queryMediator.QueryAsync(new GetCategoryQuery(id));
        if (null == cat) return NotFound();

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

        // Log activity
        await LogActivityAsync(
            EventType.CategoryCreated,
            "Create Category",
            command.DisplayName,
            new { command.Slug, command.Note });

        return Created(string.Empty, command);
    }

    [HttpPut("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([NotEmpty] Guid id, UpdateCategoryCommand command)
    {
        command.Id = id;
        var oc = await CommandMediator.SendAsync(command);
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        // Log activity
        await LogActivityAsync(
            EventType.CategoryUpdated,
            "Update Category",
            command.DisplayName,
            new { command.Id, command.Slug, command.Note });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        // Get category info before deletion for logging
        var cat = await queryMediator.QueryAsync(new GetCategoryQuery(id));

        var oc = await CommandMediator.SendAsync(new DeleteCategoryCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        // Log activity
        await LogActivityAsync(
            EventType.CategoryDeleted,
            "Delete Category",
            cat?.DisplayName ?? id.ToString(),
            new { CategoryId = id });

        return NoContent();
    }
}