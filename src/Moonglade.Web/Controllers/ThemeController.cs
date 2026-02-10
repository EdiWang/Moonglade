using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThemeController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ICacheAside cache,
    IBlogConfig blogConfig) : ControllerBase
{
    [HttpGet("/theme.css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Css()
    {
        var css = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "theme", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(20);

            var data = await queryMediator.QueryAsync(new GetSiteThemeStyleSheetQuery(blogConfig.AppearanceSettings.ThemeId));
            return data;
        });

        if (css == null) return NotFound();

        return Content(css, "text/css; charset=utf-8");
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Create(CreateThemeRequest request)
    {
        // AccentColor2 = AccentColor Lighten by 20%
        double percentage = 0.2;
        string accentColor2 = ThemeFactory.LightenColor(request.AccentColor, percentage);

        var dic = new Dictionary<string, string>
        {
            { "--accent-color1", request.AccentColor },
            { "--accent-color2", accentColor2 }
        };

        var id = await commandMediator.SendAsync(new CreateThemeCommand(request.Name, dic));
        if (id == -1) return Problem(detail: "Theme with same name already exists", statusCode: StatusCodes.Status409Conflict);

        return Ok(id);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await commandMediator.SendAsync(new DeleteThemeCommand(id));
        return oc switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => BadRequest("Can not delete system theme"),
            _ => NoContent()
        };
    }
}