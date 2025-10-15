using Edi.AspNetCore.Utils;
using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.Email.Client;
using Moonglade.Features.Asset;
using Moonglade.Web.Extensions;

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
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        blogConfig.AdvancedSettings = model;

        await SaveConfigAsync(blogConfig.AdvancedSettings);
        return NoContent();
    }

    [HttpPost("social-link")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SocialLink(SocialLinkSettingsJsonModel model)
    {
        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.JsonData))
        {
            ModelState.AddModelError(nameof(SocialLinkSettingsJsonModel.JsonData), "JsonData is required");
            return BadRequest(ModelState.GetCombinedErrorMessage());
        }

        var links = model.JsonData.FromJson<SocialLink[]>();

        // Check each link, if any link is invalid, return BadRequest
        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Name))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Name)}", "Name is required");
                return BadRequest(ModelState.GetCombinedErrorMessage());
            }

            if (string.IsNullOrWhiteSpace(link.Icon))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Icon)}", "Icon is required");
                return BadRequest(ModelState.GetCombinedErrorMessage());
            }

            if (string.IsNullOrWhiteSpace(link.Url))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Url)}", "Url is required");
                return BadRequest(ModelState.GetCombinedErrorMessage());
            }

            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out _))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Url)}", "Url is invalid");
                return BadRequest(ModelState.GetCombinedErrorMessage());
            }

            // Sterilize
            link.Url = SecurityHelper.SterilizeLink(link.Url);
        }

        blogConfig.SocialLinkSettings = new()
        {
            IsEnabled = model.IsEnabled,
            Links = links
        };

        await SaveConfigAsync(blogConfig.SocialLinkSettings);
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
            return BadRequest(ModelState.GetCombinedErrorMessage());
        }

        blogConfig.AppearanceSettings = model;

        await SaveConfigAsync(blogConfig.AppearanceSettings);
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
            return BadRequest(ModelState.GetCombinedErrorMessage());
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

        if (!oldPasswordValid) return Conflict("Old password is incorrect.");

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