using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class ThemeController(
    ICommandMediator commandMediator,
    ThemeCacheInvalidator themes,
    ILogger<ThemeController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpPost]
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
        themes.Invalidate();

        await LogActivityAsync(
            ActivityEvents.ThemeCreated,
            request.Name,
            ActivityLogDetails.Create(
                ("ThemeId", id),
                ("AccentColor", request.AccentColor)),
            targetId: id.ToString(CultureInfo.InvariantCulture));

        return Ok(id);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([Range(1, int.MaxValue)] int id)
    {
        var oc = await CommandMediator.SendAsync(new DeleteThemeCommand(id));

        if (oc == OperationCode.Done)
        {
            themes.Invalidate();
            await LogActivityAsync(
                ActivityEvents.ThemeDeleted,
                $"Theme #{id.ToString(CultureInfo.InvariantCulture)}",
                ActivityLogDetails.Create(("ThemeId", id)),
                targetId: id.ToString(CultureInfo.InvariantCulture));
        }

        return oc switch
        {
            OperationCode.ObjectNotFound => NotFound(),
            OperationCode.Canceled => BadRequest("Can not delete system theme"),
            _ => NoContent()
        };
    }
}
