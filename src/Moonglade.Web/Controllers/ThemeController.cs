using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThemeController(IMediator mediator, ICacheAside cache, IBlogConfig blogConfig) : ControllerBase
{
    [HttpGet("/theme.css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Css()
    {
        try
        {
            var css = await cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "theme", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                var data = await mediator.Send(new GetSiteThemeStyleSheetQuery(blogConfig.AppearanceSettings.ThemeId));
                return data;
            });

            if (css == null) return NotFound();

            return Content(css, "text/css; charset=utf-8");
        }
        catch (InvalidDataException e)
        {
            return Conflict(e.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<string>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Create(CreateThemeRequest request)
    {
        var dic = new Dictionary<string, string>
        {
            { "--accent-color1", request.AccentColor1 },
            { "--accent-color2", request.AccentColor2 },
            { "--accent-color3", request.AccentColor3 }
        };

        var id = await mediator.Send(new CreateThemeCommand(request.Name, dic));
        if (id == -1) return Conflict("Theme with same name already exists");

        return Ok(id);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await mediator.Send(new DeleteThemeCommand(id));
        return oc switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => BadRequest("Can not delete system theme"),
            _ => NoContent()
        };
    }
}