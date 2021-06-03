using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Caching.Filters;
using Moonglade.Configuration;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Notification.Client;
using Moonglade.Setup;
using Moonglade.Utils;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using NUglify;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        #region Private Fields

        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<SettingsController> _logger;

        #endregion

        public SettingsController(
            IBlogConfig blogConfig,
            IBlogAudit blogAudit,
            ILogger<SettingsController> logger)
        {
            _blogConfig = blogConfig;
            _blogAudit = blogAudit;

            _logger = logger;
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

                return LocalRedirect(returnUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, culture, returnUrl);
                return LocalRedirect(returnUrl);
            }
        }

        [HttpPost("general")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> General([FromForm] MagicWrapper<GeneralSettingsViewModel> wrapperModel, [FromServices] ITimeZoneResolver timeZoneResolver)
        {
            var model = wrapperModel.ViewModel;

            _blogConfig.GeneralSettings.MetaKeyword = model.MetaKeyword;
            _blogConfig.GeneralSettings.MetaDescription = model.MetaDescription;
            _blogConfig.GeneralSettings.CanonicalPrefix = model.CanonicalPrefix;
            _blogConfig.GeneralSettings.SiteTitle = model.SiteTitle;
            _blogConfig.GeneralSettings.Copyright = model.Copyright;
            _blogConfig.GeneralSettings.LogoText = model.LogoText;
            _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.SideBarOption = Enum.Parse<SideBarOption>(model.SideBarOption);
            _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.TimeZoneUtcOffset = timeZoneResolver.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
            _blogConfig.GeneralSettings.TimeZoneId = model.SelectedTimeZoneId;
            _blogConfig.GeneralSettings.ThemeFileName = model.SelectedThemeFileName;
            _blogConfig.GeneralSettings.OwnerName = model.OwnerName;
            _blogConfig.GeneralSettings.OwnerEmail = model.OwnerEmail;
            _blogConfig.GeneralSettings.Description = model.OwnerDescription;
            _blogConfig.GeneralSettings.ShortDescription = model.OwnerShortDescription;
            _blogConfig.GeneralSettings.AutoDarkLightTheme = model.AutoDarkLightTheme;

            await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);

            AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "General Settings updated.");

            return Ok();
        }

        [HttpPost("content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Content([FromForm] MagicWrapper<ContentSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            _blogConfig.ContentSettings.DisharmonyWords = model.DisharmonyWords;
            _blogConfig.ContentSettings.EnableComments = model.EnableComments;
            _blogConfig.ContentSettings.RequireCommentReview = model.RequireCommentReview;
            _blogConfig.ContentSettings.EnableWordFilter = model.EnableWordFilter;
            _blogConfig.ContentSettings.WordFilterMode = Enum.Parse<WordFilterMode>(model.WordFilterMode);
            _blogConfig.ContentSettings.UseFriendlyNotFoundImage = model.UseFriendlyNotFoundImage;
            _blogConfig.ContentSettings.PostListPageSize = model.PostListPageSize;
            _blogConfig.ContentSettings.HotTagAmount = model.HotTagAmount;
            _blogConfig.ContentSettings.EnableGravatar = model.EnableGravatar;
            _blogConfig.ContentSettings.ShowCalloutSection = model.ShowCalloutSection;
            _blogConfig.ContentSettings.CalloutSectionHtmlPitch = model.CalloutSectionHtmlPitch;
            _blogConfig.ContentSettings.ShowPostFooter = model.ShowPostFooter;
            _blogConfig.ContentSettings.PostFooterHtmlPitch = model.PostFooterHtmlPitch;
            _blogConfig.ContentSettings.DefaultLangCode = model.DefaultLangCode;

            await _blogConfig.SaveAsync(_blogConfig.ContentSettings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedContent, "Content Settings updated.");

            return Ok();
        }

        [HttpPost("notification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Notification([FromForm] MagicWrapper<NotificationSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.NotificationSettings;
            settings.EmailDisplayName = model.EmailDisplayName;
            settings.EnableEmailSending = model.EnableEmailSending;
            settings.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
            settings.SendEmailOnNewComment = model.SendEmailOnNewComment;
            settings.AzureFunctionEndpoint = model.AzureFunctionEndpoint;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedNotification, "Notification Settings updated.");

            return Ok();
        }

        [HttpPost("send-test-email")]
        [IgnoreAntiforgeryToken]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SendTestEmail([FromServices] IBlogNotificationClient notificationClient)
        {
            await notificationClient.TestNotificationAsync();
            return Ok(true);
        }

        [HttpPost("subscription")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Subscription([FromForm] MagicWrapper<SubscriptionSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.FeedSettings;
            settings.AuthorName = model.AuthorName;
            settings.RssCopyright = model.RssCopyright;
            settings.RssDescription = model.RssDescription;
            settings.RssItemCount = model.RssItemCount;
            settings.RssTitle = model.RssTitle;
            settings.UseFullContent = model.UseFullContent;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedSubscription, "Subscription Settings updated.");

            return Ok();
        }

        [HttpPost("watermark")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Watermark([FromForm] MagicWrapper<WatermarkSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.WatermarkSettings;
            settings.IsEnabled = model.IsEnabled;
            settings.KeepOriginImage = model.KeepOriginImage;
            settings.FontSize = model.FontSize;
            settings.WatermarkText = model.WatermarkText;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedWatermark, "Watermark Settings updated.");

            return Ok();
        }

        [HttpPost("set-blogger-avatar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "avatar" })]
        public async Task<IActionResult> SetBloggerAvatar([FromForm] string base64Img)
        {
            try
            {
                base64Img = base64Img.Trim();
                if (!Helper.TryParseBase64(base64Img, out var base64Chars))
                {
                    _logger.LogWarning("Bad base64 is used when setting avatar.");
                    return Conflict("Bad base64 data");
                }

                try
                {
                    using var bmp = new Bitmap(new MemoryStream(base64Chars));
                    if (bmp.Height != bmp.Width || bmp.Height + bmp.Width != 600)
                    {
                        return Conflict("Image size must be 300x300.");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Invalid base64img Image", e);
                    return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
                }

                await _blogConfig.SaveAssetAsync(AssetId.AvatarBase64, base64Img);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Avatar updated.");

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading avatar image.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("set-siteicon")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetSiteIcon([FromForm] string base64Img)
        {
            try
            {
                base64Img = base64Img.Trim();
                if (!Helper.TryParseBase64(base64Img, out var base64Chars))
                {
                    _logger.LogWarning("Bad base64 is used when setting site icon.");
                    return Conflict("Bad base64 data");
                }

                try
                {
                    using var bmp = new Bitmap(new MemoryStream(base64Chars));
                    if (bmp.Height != bmp.Width) return Conflict("image height must be equal to width");
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
                }

                await _blogConfig.SaveAssetAsync(AssetId.SiteIconBase64, base64Img);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Site icon updated.");

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading avatar image.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("advanced")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Advanced([FromForm] MagicWrapper<AdvancedSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;

            var settings = _blogConfig.AdvancedSettings;
            settings.RobotsTxtContent = model.RobotsTxtContent;
            settings.EnablePingBackSend = model.EnablePingbackSend;
            settings.EnablePingBackReceive = model.EnablePingbackReceive;
            settings.EnableOpenGraph = model.EnableOpenGraph;
            settings.EnableCDNRedirect = model.EnableCDNRedirect;
            settings.EnableOpenSearch = model.EnableOpenSearch;
            settings.FitImageToDevicePixelRatio = model.FitImageToDevicePixelRatio;
            settings.EnableMetaWeblog = model.EnableMetaWeblog;
            settings.WarnExternalLink = model.WarnExternalLink;
            settings.AllowScriptsInPage = model.AllowScriptsInPage;
            settings.ShowAdminLoginButton = model.ShowAdminLoginButton;

            if (!string.IsNullOrWhiteSpace(model.MetaWeblogPassword))
            {
                settings.MetaWeblogPasswordHash = Helper.HashPassword(model.MetaWeblogPassword);
            }

            if (model.EnableCDNRedirect)
            {
                if (string.IsNullOrWhiteSpace(model.CDNEndpoint))
                {
                    throw new ArgumentNullException(nameof(model.CDNEndpoint),
                        $"{nameof(model.CDNEndpoint)} must be specified when {nameof(model.EnableCDNRedirect)} is enabled.");
                }

                _logger.LogWarning("Images are configured to use CDN, the endpoint is out of control, use it on your own risk.");

                // Validate endpoint Url to avoid security risks
                // But it still has risks:
                // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

                var endpoint = model.CDNEndpoint;
                var isValidEndpoint = endpoint.IsValidUrl(UrlExtension.UrlScheme.Https);
                if (!isValidEndpoint)
                {
                    throw new UriFormatException("CDN Endpoint is not a valid HTTPS Url.");
                }

                settings.CDNEndpoint = model.CDNEndpoint;
            }

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Advanced Settings updated.");
            return Ok();
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

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "System reset.");

            applicationLifetime.StopApplication();
            return Accepted();
        }

        [HttpPost("custom-css")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CustomStyleSheet([FromForm] MagicWrapper<CustomStyleSheetSettingsViewModel> wrapperModel)
        {
            var model = wrapperModel.ViewModel;
            var settings = _blogConfig.CustomStyleSheetSettings;

            if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
            {
                ModelState.AddModelError(nameof(CustomStyleSheetSettingsViewModel.CssCode), "CSS Code is required");
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

            settings.EnableCustomCss = model.EnableCustomCss;
            settings.CssCode = model.CssCode;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Custom Style Sheet Settings updated.");
            return Ok();
        }

        [HttpPost("clear-data-cache")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult ClearDataCache([FromForm] string[] cachedObjectValues, [FromServices] IBlogCache cache)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState.CombineErrorMessages());

            if (cachedObjectValues.Contains("MCO_IMEM"))
            {
                cache.RemoveAllCache();
            }

            return Ok();
        }

        [HttpGet("generate-password")]
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
        public async Task<IActionResult> ClearAuditLogs()
        {
            await _blogAudit.ClearAuditLog();
            return Ok();
        }
    }
}