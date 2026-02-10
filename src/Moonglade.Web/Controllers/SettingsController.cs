using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Email.Client;
using Moonglade.Features.Asset;
using Moonglade.Web.Extensions;
using SecurityHelper = Moonglade.Utils.SecurityHelper;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SettingsController(
        IBlogConfig blogConfig,
        ILogger<SettingsController> logger,
        IEventMediator eventMediator,
        IQueryMediator queryMediator,
        ICommandMediator commandMediator) : ControllerBase
{
    [HttpPost("general")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> General(GeneralSettings model)
    {
        model.AvatarUrl = blogConfig.GeneralSettings.AvatarUrl;
        blogConfig.GeneralSettings = model;

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

    [HttpPost("comment")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Comment(CommentSettings model)
    {
        blogConfig.CommentSettings = model;

        await SaveConfigAsync(blogConfig.CommentSettings);
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
            await eventMediator.PublishAsync(new TestEmailEvent());
            return Ok(true);
        }
        catch (Exception e)
        {
            return Problem(detail: "Failed to send test email.", statusCode: StatusCodes.Status500InternalServerError);
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        await SaveConfigAsync(blogConfig.AdvancedSettings);
        return NoContent();
    }

    [HttpPost("appearance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Appearance(AppearanceSettings model)
    {
        if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
        {
            ModelState.AddModelError(nameof(AppearanceSettings.CssCode), "CSS Code is required");
            return ValidationProblem(ModelState);
        }

        blogConfig.AppearanceSettings = model;

        await SaveConfigAsync(blogConfig.AppearanceSettings);
        return NoContent();
    }

    [HttpGet("custom-menu")]
    [ProducesResponseType<CustomMenuSettings>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CustomMenu() => Ok(blogConfig.CustomMenuSettings);

    [HttpPost("custom-menu")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    [HttpPut("password/local")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
        var oldPasswordValid = blogConfig.LocalAccountSettings.PasswordHash == SecurityHelper.HashPassword(request.OldPassword.Trim(), blogConfig.LocalAccountSettings.PasswordSalt);

        if (!oldPasswordValid) return Problem(detail: "Old password is incorrect.", statusCode: StatusCodes.Status409Conflict);

        var newSalt = SecurityHelper.GenerateSalt();
        blogConfig.LocalAccountSettings.Username = request.NewUsername.Trim();
        blogConfig.LocalAccountSettings.PasswordSalt = newSalt;
        blogConfig.LocalAccountSettings.PasswordHash = SecurityHelper.HashPassword(request.NewPassword, newSalt);

        await SaveConfigAsync(blogConfig.LocalAccountSettings);
        return NoContent();
    }

    private async Task SaveConfigAsync<T>(T blogSettings) where T : IBlogSettings
    {
        var kvp = blogConfig.UpdateAsync(blogSettings);
        await commandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }
}