using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class ThemeController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ICacheAside cache,
    IBlogConfig blogConfig) : BlogControllerBase(commandMediator)
{
    [HttpGet("/theme.css")]
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

        var id = await CommandMediator.SendAsync(new CreateThemeCommand(request.Name, dic));
        if (id == -1) return Conflict("Theme with same name already exists");

        await LogActivityAsync(
            EventType.ThemeCreated,
            "Create Theme",
            request.Name,
            new { ThemeId = id, request.AccentColor });

        return Ok(id);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Delete))]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await CommandMediator.SendAsync(new DeleteThemeCommand(id));

        if (oc == OperationCode.Done)
        {
            await LogActivityAsync(
                EventType.ThemeDeleted,
                "Delete Theme",
                $"Theme #{id}",
                new { ThemeId = id });
        }

        return oc switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => BadRequest("Can not delete system theme"),
            _ => NoContent()
        };
    }
}