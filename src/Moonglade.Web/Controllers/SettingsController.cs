using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Moonglade.ActivityLog;
using Moonglade.Email;
using Moonglade.Email.Core;
using Moonglade.Features.Asset;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IEventMediator eventMediator,
        EmailCapabilityStatus emailCapabilityStatus,
        IQueryMediator queryMediator,
        ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpPost("general")]
    public async Task<IActionResult> General(GeneralSettings model)
    {
        model.AvatarUrl = blogConfig.GeneralSettings.AvatarUrl;

        await CommandMediator.SendAsync(UpdateConfigurationCommand.Create(model), HttpContext.RequestAborted);
        AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);
        await LogActivityAsync(EventType.SettingsGeneralUpdated, "Update General Settings", "General Settings",
            ActivityLogMetaData.Create(
                ("SiteTitle", model.SiteTitle),
                ("LogoText", model.LogoText)));
        return NoContent();
    }

    [HttpPost("content")]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        return await UpdateSettingsAsync(model,
            EventType.SettingsContentUpdated, "Update Content Settings", "Content Settings");
    }

    [HttpPost("comment")]
    public async Task<IActionResult> Comment(CommentSettings model)
    {
        return await UpdateSettingsAsync(model,
            EventType.SettingsCommentUpdated, "Update Comment Settings", "Comment Settings",
            ActivityLogMetaData.Create(
                ("EnableComments", model.EnableComments),
                ("RequireCommentReview", model.RequireCommentReview)));
    }

    [HttpPost("notification")]
    public async Task<IActionResult> Notification(NotificationSettings model)
    {
        return await UpdateSettingsAsync(model,
            EventType.SettingsNotificationUpdated, "Update Notification Settings", "Notification Settings");
    }

    [HttpPost("email/test")]
    public async Task<IActionResult> TestEmail()
    {
        if (!emailCapabilityStatus.IsAvailable)
        {
            return CreateEmailCapabilityProblem();
        }

        if (!blogConfig.NotificationSettings.EnableEmailSending)
        {
            return Problem(
                detail: "Enable email sending in Notification settings before sending a test email.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Email notifications are disabled.");
        }

        try
        {
            await eventMediator.PublishAsync(new TestEmailEvent());
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

    [HttpPost("subscription")]
    public async Task<IActionResult> Subscription(FeedSettings model)
    {
        return await UpdateSettingsAsync(model,
            EventType.SettingsSubscriptionUpdated, "Update Subscription Settings", "Subscription Settings");
    }

    [HttpPost("watermark")]
    public async Task<IActionResult> Image(ImageSettings model, IBlogImageStorage imageStorage)
    {
        var generalSettings = blogConfig.GeneralSettings.ToJson().FromJson<GeneralSettings>();
        var updateAvatar = false;

        if (model.EnableCDNRedirect)
        {
            if (blogConfig.GeneralSettings.AvatarUrl != null
            && !blogConfig.GeneralSettings.AvatarUrl.StartsWith(model.CDNEndpoint))
            {
                try
                {
                    var avatarData = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.AvatarBase64));

                    if (!string.IsNullOrWhiteSpace(avatarData))
                    {
                        var avatarBytes = Convert.FromBase64String(avatarData);
                        var fileName = $"avatar-{AssetId.AvatarBase64:N}.png";
                        fileName = await imageStorage.InsertAsync(fileName, avatarBytes);
                        generalSettings.AvatarUrl = model.CDNEndpoint.CombineUrl(fileName);
                        updateAvatar = true;
                    }
                }
                catch (FormatException e)
                {
                    logger.LogError(e, "Error {Action}(), Invalid Base64 string", nameof(Image));
                }
            }
        }
        else
        {
            generalSettings.AvatarUrl = Url.Action("Avatar", "Assets");
            updateAvatar = true;
        }

        var command = updateAvatar
            ? UpdateConfigurationCommand.Create(model, generalSettings)
            : UpdateConfigurationCommand.Create(model);
        await CommandMediator.SendAsync(command, HttpContext.RequestAborted);
        await LogActivityAsync(EventType.SettingsImageUpdated, "Update Image Settings", "Image Settings",
            ActivityLogMetaData.Create(
                ("EnableCDNRedirect", model.EnableCDNRedirect),
                ("IsWatermarkEnabled", model.IsWatermarkEnabled)));
        return NoContent();
    }

    [HttpPost("advanced")]
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        if (!string.IsNullOrWhiteSpace(model.HeadScripts) &&
            !ScriptTagValidator.IsValidScriptBlock(model.HeadScripts))
        {
            ModelState.AddModelError(nameof(AdvancedSettings.HeadScripts),
                "Only <script>...</script> blocks are allowed.");
            return ValidationProblem(ModelState);
        }

        if (!string.IsNullOrWhiteSpace(model.FootScripts) &&
            !ScriptTagValidator.IsValidScriptBlock(model.FootScripts))
        {
            ModelState.AddModelError(nameof(AdvancedSettings.FootScripts),
                "Only <script>...</script> blocks are allowed.");
            return ValidationProblem(ModelState);
        }

        return await UpdateSettingsAsync(model,
            EventType.SettingsAdvancedUpdated, "Update Advanced Settings", "Advanced Settings");
    }

    [HttpPost("appearance")]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Appearance(AppearanceSettings model)
    {
        if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
        {
            ModelState.AddModelError(nameof(AppearanceSettings.CssCode), "CSS Code is required");
            return ValidationProblem(ModelState);
        }

        return await UpdateSettingsAsync(model,
            EventType.SettingsAppearanceUpdated, "Update Appearance Settings", "Appearance Settings",
            ActivityLogMetaData.Create(
                ("ThemeId", model.ThemeId),
                ("EnableCustomCss", model.EnableCustomCss)));
    }

    [HttpGet("custom-menu")]
    public async Task<IActionResult> CustomMenu() => Ok(blogConfig.CustomMenuSettings);

    [HttpPost("custom-menu")]
    public async Task<IActionResult> CustomMenu(CustomMenuSettingsJsonModel model)
    {
        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.MenuJson))
        {
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Menus is required");
            return ValidationProblem(ModelState);
        }

        Menu[] menus;
        try
        {
            menus = model.MenuJson.FromJson<Menu[]>();
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON in custom menu settings");
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Invalid JSON format for menus.");
            return ValidationProblem(ModelState);
        }

        if (menus is null)
        {
            ModelState.AddModelError(nameof(CustomMenuSettingsJsonModel.MenuJson), "Invalid JSON format for menus.");
            return ValidationProblem(ModelState);
        }

        var settings = new CustomMenuSettings
        {
            IsEnabled = model.IsEnabled,
            Menus = menus
        };

        return await UpdateSettingsAsync(settings,
            EventType.SettingsCustomMenuUpdated, "Update Custom Menu Settings", "Custom Menu Settings",
            ActivityLogMetaData.Create(("IsEnabled", model.IsEnabled)));
    }

    [HttpGet("password/generate")]
    public IActionResult GeneratePassword([FromServices] IPasswordGenerator passwordGenerator)
    {
        var password = passwordGenerator.GeneratePassword();
        return Ok(new GeneratedPasswordResponse(DateTime.UtcNow, password));
    }

    [HttpPut("password/local")]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
        var oldPasswordValid = blogConfig.LocalAccountSettings.PasswordHash == SecurityHelper.HashPassword(request.OldPassword.Trim(), blogConfig.LocalAccountSettings.PasswordSalt);

        if (!oldPasswordValid) return Conflict("Old password is incorrect.");

        var newSalt = SecurityHelper.GenerateSalt();
        var newSettings = new LocalAccountSettings
        {
            Username = request.NewUsername.Trim(),
            PasswordSalt = newSalt,
            PasswordHash = SecurityHelper.HashPassword(request.NewPassword, newSalt),
            TotpSecret = blogConfig.LocalAccountSettings.TotpSecret,
            IsTotpEnabled = blogConfig.LocalAccountSettings.IsTotpEnabled
        };

        return await UpdateSettingsAsync(newSettings,
            EventType.SettingsPasswordUpdated, "Update Local Account Password", request.NewUsername,
            ActivityLogMetaData.Create(("Username", request.NewUsername)));
    }

    [HttpPut("totp/local/reset")]
    public async Task<IActionResult> ResetLocalAccountTotp(ResetLocalAccountTotpRequest request)
    {
        var account = blogConfig.LocalAccountSettings;
        var passwordValid = account.PasswordHash == SecurityHelper.HashPassword(request.CurrentPassword.Trim(), account.PasswordSalt);

        if (!passwordValid) return Conflict("Current password is incorrect.");

        var newSettings = new LocalAccountSettings
        {
            Username = account.Username,
            PasswordSalt = account.PasswordSalt,
            PasswordHash = account.PasswordHash,
            TotpSecret = string.Empty,
            IsTotpEnabled = false
        };

        await CommandMediator.SendAsync(UpdateConfigurationCommand.Create(newSettings), HttpContext.RequestAborted);
        await LogActivityAsync(
            EventType.SettingsAuthenticatorReset,
            "Reset Local Account Authenticator",
            account.Username,
            ActivityLogMetaData.Create(("Username", account.Username)));

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);

        return NoContent();
    }

    private async Task<IActionResult> UpdateSettingsAsync<T>(
        T settings,
        EventType eventType,
        string operation,
        string targetName,
        object metadata = null) where T : IBlogSettings<T>
    {
        await CommandMediator.SendAsync(UpdateConfigurationCommand.Create(settings), HttpContext.RequestAborted);
        await LogActivityAsync(eventType, operation, targetName, metadata);
        return NoContent();
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

file sealed record GeneratedPasswordResponse(DateTime ServerTimeUtc, string Password);
