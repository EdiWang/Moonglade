using NUglify;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ThemeController : ControllerBase
{
    private readonly IMediator _mediator;

    private readonly ICacheAside _cache;
    private readonly IBlogConfig _blogConfig;

    public ThemeController(IMediator mediator, ICacheAside cache, IBlogConfig blogConfig)
    {
        _mediator = mediator;

        _cache = cache;
        _blogConfig = blogConfig;
    }

    [HttpGet("/theme.css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(List<UglifyError>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Css()
    {
        try
        {
            var css = await _cache.GetOrCreateAsync(BlogCachePartition.General.ToString(), "theme", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(20);

                // Fall back to default theme for migration
                if (_blogConfig.GeneralSettings.ThemeId == 0)
                {
                    _blogConfig.GeneralSettings.ThemeId = 1;
                    var kvp = _blogConfig.UpdateAsync(_blogConfig.GeneralSettings);
                    await _mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
                }

                var data = await _mediator.Send(new GetStyleSheetQuery(_blogConfig.GeneralSettings.ThemeId));
                return data;
            });

            if (css == null) return NotFound();

            var uCss = Uglify.Css(css);
            if (uCss.HasErrors) return Conflict(uCss.Errors);

            return Content(uCss.Code, "text/css; charset=utf-8");
        }
        catch (InvalidDataException e)
        {
            return Conflict(e.Message);
        }
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCachePartition.General, "theme" })]
    public async Task<IActionResult> Create(CreateThemeRequest request)
    {
        var dic = new Dictionary<string, string>
        {
            { "--accent-color1", request.AccentColor1 },
            { "--accent-color2", request.AccentColor2 },
            { "--accent-color3", request.AccentColor3 }
        };

        var id = await _mediator.Send(new CreateThemeCommand(request.Name, dic));
        if (id == 0) return Conflict("Theme with same name already exists");

        return Ok(id);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCachePartition.General, "theme" })]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await _mediator.Send(new DeleteThemeCommand(id));
        return oc switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => BadRequest("Can not delete system theme"),
            _ => NoContent()
        };
    }
}