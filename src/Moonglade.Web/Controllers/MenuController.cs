using Moonglade.Caching.Filters;
using Moonglade.Menus;
using Moonglade.Web.Attributes;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create(EditMenuRequest model)
    {
        var request = new EditMenuRequest
        {
            DisplayOrder = model.DisplayOrder.GetValueOrDefault(),
            Icon = model.Icon,
            Title = model.Title,
            Url = model.Url,
            IsOpenInNewTab = model.IsOpenInNewTab
        };

        if (null != model.SubMenus)
        {
            var subMenuRequests = model.SubMenus
                .Select(p => new EditSubMenuRequest
                {
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                }).ToArray();

            request.SubMenus = subMenuRequests;
        }

        var response = await _mediator.Send(new CreateMenuCommand(request));
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete([NotEmpty] Guid id)
    {
        await _mediator.Send(new DeleteMenuCommand(id));
        return NoContent();
    }

    [HttpGet("edit/{id:guid}")]
    [ProducesResponseType(typeof(EditMenuRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Edit([NotEmpty] Guid id)
    {
        var menu = await _mediator.Send(new GetMenuQuery(id));
        if (null == menu) return NotFound();

        var model = new EditMenuRequest
        {
            Id = menu.Id,
            DisplayOrder = menu.DisplayOrder,
            Icon = menu.Icon,
            Title = menu.Title,
            Url = menu.Url,
            IsOpenInNewTab = menu.IsOpenInNewTab,
            SubMenus = menu.SubMenus.Select(p => new EditSubMenuRequest
            {
                Id = p.Id,
                Title = p.Title,
                Url = p.Url,
                IsOpenInNewTab = p.IsOpenInNewTab
            }).ToArray()
        };

        return Ok(model);
    }

    [HttpPut("edit")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "menu" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Edit(EditMenuRequest model)
    {
        var request = new EditMenuRequest
        {
            Title = model.Title,
            DisplayOrder = model.DisplayOrder.GetValueOrDefault(),
            Icon = model.Icon,
            Url = model.Url,
            IsOpenInNewTab = model.IsOpenInNewTab
        };

        if (null != model.SubMenus)
        {
            var subMenuRequests = model.SubMenus
                .Select(p => new EditSubMenuRequest
                {
                    Title = p.Title,
                    Url = p.Url,
                    IsOpenInNewTab = p.IsOpenInNewTab
                }).ToArray();

            request.SubMenus = subMenuRequests;
        }

        await _mediator.Send(new UpdateMenuCommand(model.Id, request));
        return NoContent();
    }
}