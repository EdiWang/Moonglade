﻿using Edi.PasswordGenerator;
using Microsoft.AspNetCore.Localization;
using Moonglade.Email.Client;

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
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Content(ContentSettings model)
    {
        blogConfig.ContentSettings = model;

        await SaveConfigAsync(blogConfig.ContentSettings);
        return NoContent();
    }

    [HttpPost("comment")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Comment(CommentSettings model)
    {
        blogConfig.CommentSettings = model;

        await SaveConfigAsync(blogConfig.CommentSettings);
        return NoContent();
    }

    [HttpPost("notification")]
    [ReadonlyMode]
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
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Subscription(FeedSettings model)
    {
        blogConfig.FeedSettings = model;

        await SaveConfigAsync(blogConfig.FeedSettings);
        return NoContent();
    }

    [HttpPost("watermark")]
    [ReadonlyMode]
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
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Advanced(AdvancedSettings model)
    {
        blogConfig.AdvancedSettings = model;

        await SaveConfigAsync(blogConfig.AdvancedSettings);
        return NoContent();
    }

    [HttpPost("social-link")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SocialLink(SocialLinkSettingsJsonModel model)
    {
        if (model.IsEnabled && string.IsNullOrWhiteSpace(model.JsonData))
        {
            ModelState.AddModelError(nameof(SocialLinkSettingsJsonModel.JsonData), "JsonData is required");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        var links = model.JsonData.FromJson<SocialLink[]>();

        // Check each link, if any link is invalid, return BadRequest
        foreach (var link in links)
        {
            if (string.IsNullOrWhiteSpace(link.Name))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Name)}", "Name is required");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (string.IsNullOrWhiteSpace(link.Icon))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Icon)}", "Icon is required");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (string.IsNullOrWhiteSpace(link.Url))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Url)}", "Url is required");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out _))
            {
                ModelState.AddModelError($"{nameof(Moonglade.Configuration.SocialLink)}.{nameof(Moonglade.Configuration.SocialLink.Url)}", "Url is invalid");
                return BadRequest(ModelState.CombineErrorMessages());
            }

            // Sterilize
            link.Url = Helper.SterilizeLink(link.Url);
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
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [TypeFilter(typeof(ClearBlogCache), Arguments = [BlogCachePartition.General, "theme"])]
    public async Task<IActionResult> Appearance(AppearanceSettings model)
    {
        if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
        {
            ModelState.AddModelError(nameof(AppearanceSettings.CssCode), "CSS Code is required");
            return BadRequest(ModelState.CombineErrorMessages());
        }

        blogConfig.AppearanceSettings = model;

        await SaveConfigAsync(blogConfig.AppearanceSettings);
        return NoContent();
    }

    [HttpPost("custom-menu")]
    [ReadonlyMode]
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

    [HttpPut("password/local")]
    [ReadonlyMode]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
        var oldPasswordValid = blogConfig.LocalAccountSettings.PasswordHash == Helper.HashPassword(request.OldPassword.Trim(), blogConfig.LocalAccountSettings.PasswordSalt);

        if (!oldPasswordValid) return Conflict("Old password is incorrect.");

        var newSalt = Helper.GenerateSalt();
        blogConfig.LocalAccountSettings.Username = request.NewUsername.Trim();
        blogConfig.LocalAccountSettings.PasswordSalt = newSalt;
        blogConfig.LocalAccountSettings.PasswordHash = Helper.HashPassword(request.NewPassword, newSalt);

        await SaveConfigAsync(blogConfig.LocalAccountSettings);
        return NoContent();
    }

    private async Task SaveConfigAsync<T>(T blogSettings) where T : IBlogSettings
    {
        var kvp = blogConfig.UpdateAsync(blogSettings);
        await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }
}