using Moonglade.Core.CategoryFeature;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Category), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([NotEmpty] Guid id)
    {
        var cat = await _mediator.Send(new GetCategoryByIdCommand(id));
        if (null == cat) return NotFound();

        return Ok(cat);
    }

    [HttpGet("list")]
    [FeatureGate(FeatureFlags.EnableWebApi)]
    [Authorize(AuthenticationSchemes = BlogAuthSchemas.All)]
    [ProducesResponseType(typeof(IReadOnlyList<Category>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var cats = await _mediator.Send(new GetCategoriesQuery());
        return Ok(cats);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(EditCategoryRequest model)
    {
        await _mediator.Send(new CreateCategoryCommand(model));
        return Created(string.Empty, model);
    }

    [HttpPut("{id:guid}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Put))]
    public async Task<IActionResult> Update([NotEmpty] Guid id, EditCategoryRequest model)
    {
        var oc = await _mediator.Send(new UpdateCategoryCommand(id, model));
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