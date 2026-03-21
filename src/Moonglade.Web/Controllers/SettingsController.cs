using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Email.Client;
using Moonglade.Features.Asset;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IEventMediator eventMediator,
        IQueryMediator queryMediator,
        ICommandMediator commandMediator) : BlogControllerBase(commandMediator)
{
    [HttpPost("general")]
    public async Task<IActionResult> General(GeneralSettings model)
    {
        model.AvatarUrl = blogConfig.GeneralSettings.AvatarUrl;
        blogConfig.GeneralSettings = model;

        AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

        return await UpdateSettingsAsync(blogConfig.GeneralSettings,
            EventType.SettingsGeneralUpdated, "Update General Settings", "General Settings",
            new { model.SiteTitle, model.LogoText });
    }

    [HttpPost("content")]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        blogConfig.ContentSettings = model;
        return await UpdateSettingsAsync(blogConfig.ContentSettings,
            EventType.SettingsContentUpdated, "Update Content Settings", "Content Settings");
    }

    [HttpPost("comment")]
    public async Task<IActionResult> Comment(CommentSettings model)
    {
        blogConfig.CommentSettings = model;
        return await UpdateSettingsAsync(blogConfig.CommentSettings,
            EventType.SettingsCommentUpdated, "Update Comment Settings", "Comment Settings",
            new { model.EnableComments, model.RequireCommentReview });
    }

    [HttpPost("notification")]
    public async Task<IActionResult> Notification(NotificationSettings model)
    {
        blogConfig.NotificationSettings = model;
        return await UpdateSettingsAsync(blogConfig.NotificationSettings,
            EventType.SettingsNotificationUpdated, "Update Notification Settings", "Notification Settings");
    }

    [HttpPost("email/test")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> TestEmail()
    {
        try
        {
            await eventMediator.PublishAsync(new TestEmailEvent());
            return Ok(true);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending test email");
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to send test email.");
        }
    }

    [HttpPost("subscription")]
    public async Task<IActionResult> Subscription(FeedSettings model)
    {
        blogConfig.FeedSettings = model;
        return await UpdateSettingsAsync(blogConfig.FeedSettings,
            EventType.SettingsSubscriptionUpdated, "Update Subscription Settings", "Subscription Settings");
    }

    [HttpPost("watermark")]
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
                    var avatarData = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.AvatarBase64));

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
                    logger.LogError(e, "Error {Action}(), Invalid Base64 string", nameof(Image));
                }
            }
        }
        else
        {
            blogConfig.GeneralSettings.AvatarUrl = Url.Action("Avatar", "Assets");
            await SaveConfigAsync(blogConfig.GeneralSettings);
        }

        return await UpdateSettingsAsync(blogConfig.ImageSettings,
            EventType.SettingsImageUpdated, "Update Image Settings", "Image Settings",
            new { model.EnableCDNRedirect, model.IsWatermarkEnabled });
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

        blogConfig.AdvancedSettings = model;
        return await UpdateSettingsAsync(blogConfig.AdvancedSettings,
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

        blogConfig.AppearanceSettings = model;
        return await UpdateSettingsAsync(blogConfig.AppearanceSettings,
            EventType.SettingsAppearanceUpdated, "Update Appearance Settings", "Appearance Settings",
            new { model.ThemeId, model.EnableCustomCss });
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

        blogConfig.CustomMenuSettings = new()
        {
            IsEnabled = model.IsEnabled,
            Menus = model.MenuJson.FromJson<Menu[]>()
        };

        return await UpdateSettingsAsync(blogConfig.CustomMenuSettings,
            EventType.SettingsCustomMenuUpdated, "Update Custom Menu Settings", "Custom Menu Settings",
            new { model.IsEnabled });
    }

    [HttpGet("password/generate")]
    public IActionResult GeneratePassword([FromServices] IPasswordGenerator passwordGenerator)
    {
        var password = passwordGenerator.GeneratePassword(new(10, 3));
        return Ok(new
        {
            ServerTimeUtc = DateTime.UtcNow,
            Password = password
        });
    }

    [HttpPut("password/local")]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
        var oldPasswordValid = blogConfig.LocalAccountSettings.PasswordHash == SecurityHelper.HashPassword(request.OldPassword.Trim(), blogConfig.LocalAccountSettings.PasswordSalt);

        if (!oldPasswordValid) return Conflict("Old password is incorrect.");

        var newSalt = SecurityHelper.GenerateSalt();
        blogConfig.LocalAccountSettings.Username = request.NewUsername.Trim();
        blogConfig.LocalAccountSettings.PasswordSalt = newSalt;
        blogConfig.LocalAccountSettings.PasswordHash = SecurityHelper.HashPassword(request.NewPassword, newSalt);

        return await UpdateSettingsAsync(blogConfig.LocalAccountSettings,
            EventType.SettingsPasswordUpdated, "Update Local Account Password", request.NewUsername,
            new { Username = request.NewUsername });
    }

    private async Task<IActionResult> UpdateSettingsAsync<T>(
        T settings,
        EventType eventType,
        string operation,
        string targetName,
        object metadata = null) where T : IBlogSettings
    {
        await SaveConfigAsync(settings);
        await LogActivityAsync(eventType, operation, targetName, metadata);
        return NoContent();
    }

    private async Task SaveConfigAsync<T>(T settings) where T : IBlogSettings
    {
        var kvp = blogConfig.UpdateAsync(settings);
        await CommandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }
}