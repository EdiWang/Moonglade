using Edi.PasswordGenerator;
using Microsoft.AspNetCore.Localization;
using Moonglade.Email.Client;
using NUglify;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("set-lang")]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(culture)) return BadRequest();

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new(culture)),
                new() { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "~/" : returnUrl);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message, culture, returnUrl);

            // We shall not respect the return URL now, because the returnUrl might be hacking.
            return NoContent();
        }
    }

    [HttpPost("general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { BlogCachePartition.General, "theme" })]
    public async Task<IActionResult> General(GeneralSettings model, ITimeZoneResolver timeZoneResolver)
    {
        model.AvatarUrl = blogConfig.GeneralSettings.AvatarUrl;

        blogConfig.GeneralSettings = model;
        blogConfig.GeneralSettings.TimeZoneUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(model.TimeZoneId);

        await SaveConfigAsync(blogConfig.GeneralSettings);

        AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

        return NoContent();
    }

    [HttpPost("content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        blogConfig.ContentSettings = model;

        await SaveConfigAsync(blogConfig.ContentSettings);
        return NoContent();
    }

    [HttpPost("notification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Notification(NotificationSettings model)
    {
        blogConfig.NotificationSettings = model;

        await SaveConfigAsync(blogConfig.NotificationSettings);
        return NoContent();
    }

    [HttpPost("email/test")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestEmail()
    {
        try
        {
            await mediator.Publish(new TestNotification());
            return Ok(true);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost("subscription")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Subscription(FeedSettings model)
    {
        blogConfig.FeedSettings = model;

        await SaveConfigAsync(blogConfig.FeedSettings);
        return NoContent();
    }

    [HttpPost("watermark")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Image(ImageSettings model, IBlogImageStorage imageStorage)
    {
        blogConfig.ImageSettings = model;

        if (model.EnableCDNRedirect)
        {
            if (null != blogConfig.GeneralSettings.AvatarUrl
            && !blogConfig.GeneralSettings.AvatarUrl.StartsWith(model.CDNEndpoint))
            {
                try
                {
                    var avatarData = await mediator.Send(new GetAssetQuery(AssetId.AvatarBase64));

                    if (!string.IsNullOrWhiteSpace(avatarData))
                    {
                        var avatarBytes = Convert.FromBase64String(avatarData);
                        var fileName = $"avatar-{AssetId.AvatarBase64:N}.png";
                        fileName = await imageStorage.InsertAsync(fileName, avatarBytes);
                        blogConfig.GeneralSettings.AvatarUrl = blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName);

                        await SaveConfigAsync(blogConfig.GeneralSettings);
                    }
                }
                catch (FormatException e)
                {
                    logger.LogError(e, $"Error {nameof(Image)}(), Invalid Base64 string");
                }
            }
        }
        else
        {
            blogConfig.GeneralSettings.AvatarUrl = Url.Action("Avatar", "Assets");
            await SaveConfigAsync(blogConfig.GeneralSettings);
        }

        await SaveConfigAsync(blogConfig.ImageSettings);

        return NoContent();
    }

    [HttpPost("advanced")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        model.MetaWeblogPasswordHash = !string.IsNullOrWhiteSpace(model.MetaWeblogPassword) ?
            Helper.HashPassword(model.MetaWeblogPassword) :
            blogConfig.AdvancedSettings.MetaWeblogPasswordHash;

        blogConfig.AdvancedSettings = model;

        await SaveConfigAsync(blogConfig.AdvancedSettings);
        return NoContent();
    }

    [HttpPost("shutdown")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult Shutdown(IHostApplicationLifetime applicationLifetime)
    {
        logger.LogWarning($"Shutdown is requested by '{User.Identity?.Name}'.");
        applicationLifetime.StopApplication();
        return Accepted();
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reset(BlogDbContext context, IHostApplicationLifetime applicationLifetime)
    {
        logger.LogWarning($"System reset is requested by '{User.Identity?.Name}', IP: {Helper.GetClientIP(HttpContext)}.");

        await context.ClearAllData();

        applicationLifetime.StopApplication();
        return Accepted();
    }

    [HttpPost("custom-css")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CustomStyleSheet(CustomStyleSheetSettings model)
    {
        if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
        {
            ModelState.AddModelError(nameof(CustomStyleSheetSettings.CssCode), "CSS Code is required");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        var uglifyTest = Uglify.Css(model.CssCode);
        if (uglifyTest.HasErrors)
        {
            foreach (var err in uglifyTest.Errors)
            {
                ModelState.AddModelError(model.CssCode, err.ToString());
            }
            return BadRequest(ModelState.CombineErrorMessages());
        }

        blogConfig.CustomStyleSheetSettings = model;

        await SaveConfigAsync(blogConfig.CustomStyleSheetSettings);
        return NoContent();
    }

    [HttpPost("custom-menu")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CustomMenu(CustomMenuSettingsJsonModel model)
    {
        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.MenuJson))
        {
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Menus is required");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        blogConfig.CustomMenuSettings = new()
        {
            IsEnabled = model.IsEnabled,
            Menus = model.MenuJson.FromJson<Menu[]>()
        };

        await SaveConfigAsync(blogConfig.CustomMenuSettings);
        return NoContent();
    }

    [HttpGet("password/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GeneratePassword([FromServices] IPasswordGenerator passwordGenerator)
    {
        var password = passwordGenerator.GeneratePassword(new(10, 3));
        return Ok(new
        {
            ServerTimeUtc = DateTime.UtcNow,
            Password = password
        });
    }

    private async Task SaveConfigAsync<T>(T blogSettings) where T : IBlogSettings
    {
        var kvp = blogConfig.UpdateAsync(blogSettings);
        await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }
}