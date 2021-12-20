﻿using Microsoft.AspNetCore.Localization;
using Moonglade.Caching.Filters;
using Moonglade.Data.Entities;
using Moonglade.Data.Setup;
using Moonglade.Notification.Client;
using NUglify;
using System.Data;
using System.Reflection;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    #region Private Fields

    private readonly IMediator _mediator;
    private readonly IBlogConfig _blogConfig;
    private readonly IBlogAudit _blogAudit;
    private readonly ILogger<SettingsController> _logger;

    #endregion

    public SettingsController(
        IBlogConfig blogConfig,
        IBlogAudit blogAudit,
        ILogger<SettingsController> logger,
        IMediator mediator)
    {
        _blogConfig = blogConfig;
        _blogAudit = blogAudit;

        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("release/check")]
    [ProducesResponseType(typeof(CheckNewReleaseResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckNewRelease([FromServices] IReleaseCheckerClient releaseCheckerClient)
    {
        var info = await releaseCheckerClient.CheckNewReleaseAsync();

        var asm = Assembly.GetEntryAssembly();
        var currentVersion = new Version(asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version);
        var latestVersion = new Version(info.TagName.Replace("v", string.Empty));

        var hasNewVersion = latestVersion > currentVersion && !info.PreRelease;

        var result = new CheckNewReleaseResult
        {
            HasNewRelease = hasNewVersion,
            CurrentAssemblyFileVersion = currentVersion.ToString(),
            LatestReleaseInfo = info
        };

        return Ok(result);
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
            return LocalRedirect("~/");
        }
    }

    [HttpPost("general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "theme" })]
    public async Task<IActionResult> General([FromForm] MagicWrapper<GeneralSettings> wrapperModel, [FromServices] ITimeZoneResolver timeZoneResolver)
    {
        var model = wrapperModel.ViewModel;

        _blogConfig.GeneralSettings = model;
        _blogConfig.GeneralSettings.TimeZoneUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(model.TimeZoneId).ToString();

        await SaveAsync(_blogConfig.GeneralSettings);

        AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedGeneral, "General Settings updated.");

        return NoContent();
    }

    [HttpPost("content")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Content([FromForm] MagicWrapper<ContentSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;
        _blogConfig.ContentSettings = model;

        await SaveAsync(_blogConfig.ContentSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedContent, "Content Settings updated.");

        return NoContent();
    }

    [HttpPost("notification")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Notification([FromForm] MagicWrapper<NotificationSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;
        _blogConfig.NotificationSettings = model;

        await SaveAsync(_blogConfig.NotificationSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedNotification, "Notification Settings updated.");

        return NoContent();
    }

    [HttpPost("test-email")]
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
    public async Task<IActionResult> Subscription([FromForm] MagicWrapper<FeedSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;
        _blogConfig.FeedSettings = model;

        await SaveAsync(_blogConfig.FeedSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedSubscription, "Subscription Settings updated.");

        return NoContent();
    }

    [HttpPost("watermark")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Image([FromForm] MagicWrapper<ImageSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;
        _blogConfig.ImageSettings = model;

        await SaveAsync(_blogConfig.ImageSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedImage, "Image Settings updated.");

        return NoContent();
    }

    [HttpPost("advanced")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Advanced([FromForm] MagicWrapper<AdvancedSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;

        model.MetaWeblogPasswordHash = !string.IsNullOrWhiteSpace(model.MetaWeblogPassword) ?
            Helper.HashPassword(model.MetaWeblogPassword) :
            _blogConfig.AdvancedSettings.MetaWeblogPasswordHash;

        _blogConfig.AdvancedSettings = model;

        await SaveAsync(_blogConfig.AdvancedSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "Advanced Settings updated.");
        return NoContent();
    }

    [HttpPost("shutdown")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult Shutdown([FromServices] IHostApplicationLifetime applicationLifetime)
    {
        _logger.LogWarning($"Shutdown is requested by '{User.Identity?.Name}'.");
        applicationLifetime.StopApplication();
        return Accepted();
    }

    [HttpPost("reset")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Reset([FromServices] IDbConnection dbConnection, [FromServices] IHostApplicationLifetime applicationLifetime)
    {
        _logger.LogWarning($"System reset is requested by '{User.Identity?.Name}', IP: {HttpContext.Connection.RemoteIpAddress}.");

        var setupHelper = new SetupRunner(dbConnection);
        setupHelper.ClearData();

        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "System reset.");

        applicationLifetime.StopApplication();
        return Accepted();
    }

    [HttpPost("custom-css")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CustomStyleSheet([FromForm] MagicWrapper<CustomStyleSheetSettings> wrapperModel)
    {
        var model = wrapperModel.ViewModel;

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

        await SaveAsync(_blogConfig.CustomStyleSheetSettings);
        await _blogAudit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsSavedAdvanced, "Custom Style Sheet Settings updated.");
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

    [HttpDelete("auditlogs/clear")]
    [FeatureGate(FeatureFlags.EnableAudit)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAuditLogs()
    {
        await _blogAudit.ClearAuditLog();
        return NoContent();
    }

    private async Task SaveAsync(IBlogSettings settings)
    {
        await _mediator.Send(new SaveBlogConfigurationCommand(settings));

        var configDic = await _mediator.Send(new GetAllConfigurationsQuery());
        _blogConfig.Initialize(configDic);
    }

    public class CheckNewReleaseResult
    {
        public bool HasNewRelease { get; set; }

        public ReleaseInfo LatestReleaseInfo { get; set; }
        public string CurrentAssemblyFileVersion { get; set; }
    }
}