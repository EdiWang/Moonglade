using Moonglade.Core.CategoryFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoryController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var cat = await _mediator.Send(new GetCategoryByIdQuery(id));
        if (null == cat) return NotFound();

        return Ok(cat);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateCategoryCommand command)
    {
        await _mediator.Send(command);
        return Created(string.Empty, command);
    }

    [HttpPut("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([NotEmpty] Guid id, UpdateCategoryCommand command)
    {
        command.Id = id;
        var oc = await _mediator.Send<OperationCode>(command);
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        var oc = await _mediator.Send(new DeleteCategoryCommand(id));
        if (oc == OperationCode.ObjectNotFound) return NotFound();

        return NoContent();
    }
}