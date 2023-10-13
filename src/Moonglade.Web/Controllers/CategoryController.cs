using Moonglade.Core.CategoryFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType<Category>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var cat = await mediator.Send(new GetCategoryQuery(id));
        if (null == cat) return NotFound();

        return Ok(cat);
    }

    [HttpGet("list")]
    [ProducesResponseType<IReadOnlyList<Category>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var list = await mediator.Send(new GetCategoriesQuery());
        return Ok(list);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        await mediator.Send(command);
        return Created(string.Empty, command);
    }

    [HttpPut("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([NotEmpty] Guid id, UpdateCategoryCommand command)
    {
        command.Id = id;
        var oc = await mediator.Send<OperationCode>(command);
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var oc = await mediator.Send(new DeleteCategoryCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}