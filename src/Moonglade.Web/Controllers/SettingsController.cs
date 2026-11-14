#nullable enable annotations

using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Email;
using Moonglade.Email.Core;
using System.Collections.Immutable;
using System.Text.Json;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
[SettingsValidation]
public class SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IEventMediator eventMediator,
        EmailCapabilityStatus emailCapabilityStatus,
        ICommandMediator commandMediator,
        IOwnerProfileReader ownerProfiles) : BlogControllerBase(commandMediator, logger)
{
    [HttpPost("general")]
    public async Task<IActionResult> General(GeneralSettings model)
    {
        return await UpdateSettingsAsync(model);
    }
    [HttpPost("content")]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("comment")]
    public async Task<IActionResult> Comment(CommentSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("notification")]
    public async Task<IActionResult> Notification(NotificationSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("email/test")]
    public async Task<IActionResult> TestEmail()
    {
        var settings = blogConfig.Capture();
        if (!emailCapabilityStatus.IsAvailable)
        {
            return CreateEmailCapabilityProblem();
        }

        if (!settings.NotificationSettings.EnableEmailSending)
        {
            return Problem(
                detail: "Enable email sending in Notification settings before sending a test email.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Email notifications are disabled.");
        }

        try
        {
            var owner = await ownerProfiles.ReadProfileAsync(HttpContext.RequestAborted);
            if (owner is null)
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    title: "Owner email is not configured.", detail: "Set an owner email in Account before sending a test email.");
            await eventMediator.PublishAsync(new TestEmailEvent { Snapshot = settings, Owner = owner });
            return Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error queuing test email");
            return Problem(
                detail: "The test email could not be queued.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Failed to queue test email.");
        }
    }

    [HttpPost("seo")]
    public async Task<IActionResult> Seo(SEOSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("subscription")]
    public async Task<IActionResult> Subscription(FeedSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("watermark")]
    public async Task<IActionResult> Image(ImageSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("advanced")]
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPost("appearance")]
    public async Task<IActionResult> Appearance(AppearanceSettings model)
    {
        return await UpdateSettingsAsync(model);
    }

    [HttpPatch("{group}")]
    public async Task<IActionResult> Patch(string group, JsonElement fields)
    {
        var command = group switch
        {
            "general" => PatchConfigurationCommand.Create<GeneralSettings>(static value => value with { }, fields,
                nameof(GeneralSettings.LogoText), nameof(GeneralSettings.Copyright),
                nameof(GeneralSettings.SiteTitle),
                nameof(GeneralSettings.SideBarOption), nameof(GeneralSettings.WidgetsProfile),
                nameof(GeneralSettings.HotTagAmount), nameof(GeneralSettings.WidgetsCategoryList),
                nameof(GeneralSettings.WidgetsSubscriptionButtons),
                nameof(GeneralSettings.DefaultLanguageCode)),
            "content" => PatchConfigurationCommand.Create<ContentSettings>(static value => value with { }, fields,
                nameof(ContentSettings.DefaultEditor), nameof(ContentSettings.PostListPageSize),
                nameof(ContentSettings.PostTitleAlignment), nameof(ContentSettings.CalloutSectionHtmlPitch),
                nameof(ContentSettings.ShowCalloutSection), nameof(ContentSettings.ShowPostFooter),
                nameof(ContentSettings.PostFooterHtmlPitch), nameof(ContentSettings.DocumentOutline),
                nameof(ContentSettings.EnableViewCount), nameof(ContentSettings.MaximumPageNumbersToDisplay)),
            "comment" => PatchConfigurationCommand.Create<CommentSettings>(static value => value with { }, fields,
                nameof(CommentSettings.CommentProvider), nameof(CommentSettings.CommentOrder),
                nameof(CommentSettings.ThirdPartyCommentHtmlPitch), nameof(CommentSettings.EnableComments),
                nameof(CommentSettings.EnableGravatar), nameof(CommentSettings.EnableMarkdownLinks),
                nameof(CommentSettings.RequireCommentReview), nameof(CommentSettings.CloseCommentAfterDays),
                nameof(CommentSettings.EnableWordFilter), nameof(CommentSettings.WordFilterMode),
                nameof(CommentSettings.WordFilterKeywords), nameof(CommentSettings.EnableCommentSectionOnMobile)),
            "notification" => PatchConfigurationCommand.Create<NotificationSettings>(static value => value with { }, fields,
                nameof(NotificationSettings.EnableEmailSending), nameof(NotificationSettings.SendEmailOnCommentReply),
                nameof(NotificationSettings.SendEmailOnNewComment), nameof(NotificationSettings.EmailDisplayName)),
            "subscription" => PatchConfigurationCommand.Create<FeedSettings>(static value => value with { }, fields,
                nameof(FeedSettings.FeedItemCount), nameof(FeedSettings.UseFullContent)),
            "watermark" => PatchConfigurationCommand.Create<ImageSettings>(static value => value with { }, fields,
                nameof(ImageSettings.IsWatermarkEnabled), nameof(ImageSettings.KeepOriginImage),
                nameof(ImageSettings.WatermarkFontSize), nameof(ImageSettings.WatermarkText),
                nameof(ImageSettings.WatermarkColorA), nameof(ImageSettings.WatermarkSkipPixel),
                nameof(ImageSettings.EnableCDNRedirect), nameof(ImageSettings.CDNEndpoint)),
            "advanced" => PatchConfigurationCommand.Create<AdvancedSettings>(static value => value with { }, fields,
                nameof(AdvancedSettings.HeadScripts), nameof(AdvancedSettings.FootScripts),
                nameof(AdvancedSettings.EnableWebmention), nameof(AdvancedSettings.EnableOpenSearch),
                nameof(AdvancedSettings.EnableFoaf), nameof(AdvancedSettings.EnableOpml),
                nameof(AdvancedSettings.EnablePostScheduler),
                nameof(AdvancedSettings.WarnPostExternalLink), nameof(AdvancedSettings.WarnCommentExternalLink)),
            "seo" => PatchConfigurationCommand.Create<SEOSettings>(static value => value with { }, fields,
                nameof(SEOSettings.CanonicalPrefix), nameof(SEOSettings.MetaKeyword),
                nameof(SEOSettings.EnableSiteMap), nameof(SEOSettings.EnableJsonLdBlogPosting),
                nameof(SEOSettings.UseDublinCoreMetaData), nameof(SEOSettings.DcLicenseUrl),
                nameof(SEOSettings.RobotsTxtContent)),
            "appearance" => PatchConfigurationCommand.Create<AppearanceSettings>(static value => value with { }, fields,
                nameof(AppearanceSettings.ThemeId), nameof(AppearanceSettings.EnableCustomCss),
                nameof(AppearanceSettings.CssCode)),
            _ => null
        };

        if (command is null) return NotFound();

        var updated = await CommandMediator.SendAsync(command, HttpContext.RequestAborted);
        return await CompleteSettingsSaveAsync(updated);
    }

    private async Task<IActionResult> CompleteSettingsSaveAsync(ConfigurationSaveResult saved)
    {
        if (!saved.HasEffectiveChanges) return NoContent();

        var eventKey = saved.SettingsType.Name switch
        {
            nameof(GeneralSettings) => ActivityEvents.SettingsGeneralUpdated,
            nameof(CommentSettings) => ActivityEvents.SettingsCommentUpdated,
            nameof(ImageSettings) => ActivityEvents.SettingsImageUpdated,
            nameof(AppearanceSettings) => ActivityEvents.SettingsAppearanceUpdated,
            nameof(ContentSettings) => ActivityEvents.SettingsContentUpdated,
            nameof(NotificationSettings) => ActivityEvents.SettingsNotificationUpdated,
            nameof(SEOSettings) => ActivityEvents.SettingsSeoUpdated,
            nameof(FeedSettings) => ActivityEvents.SettingsSubscriptionUpdated,
            nameof(AdvancedSettings) => ActivityEvents.SettingsAdvancedUpdated,
            nameof(CustomMenuSettings) => ActivityEvents.SettingsCustomMenuUpdated,
            _ => throw new InvalidOperationException("Unexpected settings type.")
        };

        var changes = saved.Changes.Select(change => new ActivityLogSettingChange(change.Key, change.OldValue, change.NewValue)).ToArray();
        await LogActivityAsync(eventKey, null,
            ActivityLogDetails.Create((ActivityLogSettingChange.DetailsKey, changes)));
        return NoContent();
    }

    [HttpGet("custom-menu")]
    public async Task<IActionResult> CustomMenu() => Ok(blogConfig.Capture().CustomMenuSettings);

    [HttpPost("custom-menu")]
    public async Task<IActionResult> CustomMenu(CustomMenuSettingsJsonModel model)
    {
        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.MenuJson))
        {
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Menus is required");
            return ValidationProblem(ModelState);
        }

        ImmutableList<Menu> menus;
        try
        {
            menus = string.IsNullOrWhiteSpace(model.MenuJson) ? null : model.MenuJson.FromJson<ImmutableList<Menu>>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON in custom menu settings");
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Invalid JSON format for menus.");
            return ValidationProblem(ModelState);
        }

        var settings = new CustomMenuSettings
        {
            IsEnabled = model.IsEnabled,
            Menus = menus
        };

        return await UpdateSettingsAsync(settings);
    }

    private async Task<IActionResult> UpdateSettingsAsync<T>(T settings) where T : IBlogSettings<T>
    {
        var saved = await CommandMediator.SendAsync(UpdateConfigurationCommand.Create(settings), HttpContext.RequestAborted);
        return await CompleteSettingsSaveAsync(saved);
    }

    private IActionResult CreateEmailCapabilityProblem()
    {
        return emailCapabilityStatus.State switch
        {
            EmailCapabilityState.NotConfigured => Problem(
                detail: "Add the required Email provider settings to enable email notifications.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Email notifications are not configured."),

            EmailCapabilityState.Invalid => Problem(
                detail: string.Join(" ", emailCapabilityStatus.ValidationErrors),
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Email notification configuration is invalid."),

            EmailCapabilityState.Disabled => Problem(
                detail: "Set Email:Enabled to true before sending a test email.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Email notifications are disabled."),

            _ => Problem(
                detail: "Email notifications are currently unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Email notifications are unavailable.")
        };
    }
}
