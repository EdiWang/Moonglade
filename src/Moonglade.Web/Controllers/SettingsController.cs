﻿using Microsoft.AspNetCore.Localization;
using Moonglade.Caching.Filters;
using Moonglade.Notification.Client;
using NUglify;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    #region Private Fields

    private readonly IMediator _mediator;
    private readonly IBlogConfig _blogConfig;
    private readonly ILogger<SettingsController> _logger;

    #endregion

    public SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IMediator mediator)
    {
        _blogConfig = blogConfig;
        _logger = logger;
        _mediator = mediator;
    }

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
            _logger.LogError(e, e.Message, culture, returnUrl);

            // We shall not respect the return URL now, because the returnUrl might be hacking.
            return NoContent();
        }
    }

    [HttpPost("general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
    public async Task<IActionResult> General(GeneralSettings model, ITimeZoneResolver timeZoneResolver)
    {
        model.AvatarUrl = _blogConfig.GeneralSettings.AvatarUrl;

        _blogConfig.GeneralSettings = model;
        _blogConfig.GeneralSettings.TimeZoneUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(model.TimeZoneId);

        await SaveConfigAsync(_blogConfig.GeneralSettings);

        AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

        return NoContent();
    }

    [HttpPost("content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        _blogConfig.ContentSettings = model;

        await SaveConfigAsync(_blogConfig.ContentSettings);
        return NoContent();
    }

    [HttpPost("notification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Notification(NotificationSettings model)
    {
        if (model.EnableEmailSending && string.IsNullOrWhiteSpace(model.AzureStorageQueueConnection))
        {
            ModelState.AddModelError(nameof(model.AzureStorageQueueConnection), "Azure Storage Queue Connection is required.");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        _blogConfig.NotificationSettings = model;

        await SaveConfigAsync(_blogConfig.NotificationSettings);
        return NoContent();
    }

    [HttpPost("email/test")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestEmail()
    {
        try
        {
            await _mediator.Publish(new TestNotification());
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
        _blogConfig.FeedSettings = model;

        await SaveConfigAsync(_blogConfig.FeedSettings);
        return NoContent();
    }

    [HttpPost("watermark")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Image(ImageSettings model, IBlogImageStorage imageStorage)
    {
        _blogConfig.ImageSettings = model;

        if (model.EnableCDNRedirect)
        {
            if (null != _blogConfig.GeneralSettings.AvatarUrl
            && !_blogConfig.GeneralSettings.AvatarUrl.StartsWith(model.CDNEndpoint))
            {
                try
                {
                    var avatarData = await _mediator.Send(new GetAssetQuery(AssetId.AvatarBase64));

                    if (!string.IsNullOrWhiteSpace(avatarData))
                    {
                        var avatarBytes = Convert.FromBase64String(avatarData);
                        var fileName = $"avatar-{AssetId.AvatarBase64:N}.png";
                        fileName = await imageStorage.InsertAsync(fileName, avatarBytes);
                        _blogConfig.GeneralSettings.AvatarUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName);

                        await SaveConfigAsync(_blogConfig.GeneralSettings);
                    }
                }
                catch (FormatException e)
                {
                    _logger.LogError($"Error {nameof(Image)}(), Invalid Base64 string", e);
                }
            }
        }
        else
        {
            _blogConfig.GeneralSettings.AvatarUrl = Url.Action("Avatar", "Assets");
            await SaveConfigAsync(_blogConfig.GeneralSettings);
        }

        await SaveConfigAsync(_blogConfig.ImageSettings);

        return NoContent();
    }

    [HttpPost("advanced")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        model.MetaWeblogPasswordHash = !string.IsNullOrWhiteSpace(model.MetaWeblogPassword) ?
            Helper.HashPassword(model.MetaWeblogPassword) :
            _blogConfig.AdvancedSettings.MetaWeblogPasswordHash;

        _blogConfig.AdvancedSettings = model;

        await SaveConfigAsync(_blogConfig.AdvancedSettings);
        return NoContent();
    }

    [HttpPost("shutdown")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult Shutdown(IHostApplicationLifetime applicationLifetime)
    {
        _logger.LogWarning($"Shutdown is requested by '{User.Identity?.Name}'.");
        applicationLifetime.StopApplication();
        return Accepted();
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reset(BlogDbContext context, IHostApplicationLifetime applicationLifetime)
    {
        _logger.LogWarning($"System reset is requested by '{User.Identity?.Name}', IP: {Helper.GetClientIP(HttpContext)}.");

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

        _blogConfig.CustomStyleSheetSettings = model;

        await SaveConfigAsync(_blogConfig.CustomStyleSheetSettings);
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

        _blogConfig.CustomMenuSettings = new()
        {
            IsEnabled = model.IsEnabled,
            Menus = model.MenuJson.FromJson<Menu[]>()
        };

        await SaveConfigAsync(_blogConfig.CustomMenuSettings);
        return NoContent();
    }

    [HttpGet("password/generate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GeneratePassword()
    {
        var password = Helper.GeneratePassword(10, 3);
        return Ok(new
        {
            ServerTimeUtc = DateTime.UtcNow,
            Password = password
        });
    }

    private async Task SaveConfigAsync<T>(T blogSettings) where T : IBlogSettings
    {
        var kvp = _blogConfig.UpdateAsync(blogSettings);
        await _mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }
}