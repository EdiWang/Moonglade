using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.DataPorting;
using Moonglade.FriendLink;
using Moonglade.Notification.Client;
using Moonglade.Setup;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moonglade.Web.Models.Settings;
using NUglify;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin/settings")]
    public class SettingsController : Controller
    {
        #region Private Fields

        private readonly IFriendLinkService _friendLinkService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<SettingsController> _logger;

        private static string SiteIconDirectory => Path.Join(AppDomain.CurrentDomain.GetData("DataDirectory")?.ToString(), "siteicons");

        #endregion

        public SettingsController(
            IFriendLinkService friendLinkService,
            IBlogConfig blogConfig,
            IBlogAudit blogAudit,
            ILogger<SettingsController> logger)
        {
            _blogConfig = blogConfig;
            _blogAudit = blogAudit;

            _friendLinkService = friendLinkService;
            _logger = logger;
        }

        [HttpGet("general")]
        public IActionResult General([FromServices] ITZoneResolver tZoneResolver)
        {
            var vm = new GeneralSettingsViewModel
            {
                LogoText = _blogConfig.GeneralSettings.LogoText,
                MetaKeyword = _blogConfig.GeneralSettings.MetaKeyword,
                MetaDescription = _blogConfig.GeneralSettings.MetaDescription,
                CanonicalPrefix = _blogConfig.GeneralSettings.CanonicalPrefix,
                SiteTitle = _blogConfig.GeneralSettings.SiteTitle,
                Copyright = _blogConfig.GeneralSettings.Copyright,
                SideBarCustomizedHtmlPitch = _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch,
                SideBarOption = _blogConfig.GeneralSettings.SideBarOption.ToString(),
                FooterCustomizedHtmlPitch = _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch,
                OwnerName = _blogConfig.GeneralSettings.OwnerName,
                OwnerEmail = _blogConfig.GeneralSettings.OwnerEmail,
                OwnerDescription = _blogConfig.GeneralSettings.Description,
                OwnerShortDescription = _blogConfig.GeneralSettings.ShortDescription,
                SelectedTimeZoneId = _blogConfig.GeneralSettings.TimeZoneId,
                SelectedUtcOffset = tZoneResolver.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId),
                SelectedThemeFileName = _blogConfig.GeneralSettings.ThemeFileName,
                AutoDarkLightTheme = _blogConfig.GeneralSettings.AutoDarkLightTheme
            };
            return View(vm);
        }

        [HttpPost("general")]
        public async Task<IActionResult> General(GeneralSettingsViewModel model, [FromServices] ITZoneResolver tZoneResolver)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _blogConfig.GeneralSettings.MetaKeyword = model.MetaKeyword;
            _blogConfig.GeneralSettings.MetaDescription = model.MetaDescription;
            _blogConfig.GeneralSettings.CanonicalPrefix = model.CanonicalPrefix;
            _blogConfig.GeneralSettings.SiteTitle = model.SiteTitle;
            _blogConfig.GeneralSettings.Copyright = model.Copyright;
            _blogConfig.GeneralSettings.LogoText = model.LogoText;
            _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.SideBarOption = Enum.Parse<SideBarOption>(model.SideBarOption);
            _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.TimeZoneUtcOffset = tZoneResolver.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
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

        [HttpGet("content")]
        public IActionResult Content()
        {
            var vm = new ContentSettingsViewModel
            {
                DisharmonyWords = _blogConfig.ContentSettings.DisharmonyWords,
                EnableComments = _blogConfig.ContentSettings.EnableComments,
                RequireCommentReview = _blogConfig.ContentSettings.RequireCommentReview,
                EnableWordFilter = _blogConfig.ContentSettings.EnableWordFilter,
                WordFilterMode = _blogConfig.ContentSettings.WordFilterMode.ToString(),
                UseFriendlyNotFoundImage = _blogConfig.ContentSettings.UseFriendlyNotFoundImage,
                PostListPageSize = _blogConfig.ContentSettings.PostListPageSize,
                HotTagAmount = _blogConfig.ContentSettings.HotTagAmount,
                EnableGravatar = _blogConfig.ContentSettings.EnableGravatar,
                ShowCalloutSection = _blogConfig.ContentSettings.ShowCalloutSection,
                CalloutSectionHtmlPitch = _blogConfig.ContentSettings.CalloutSectionHtmlPitch,
                ShowPostFooter = _blogConfig.ContentSettings.ShowPostFooter,
                PostFooterHtmlPitch = _blogConfig.ContentSettings.PostFooterHtmlPitch,
                DefaultLangCode = _blogConfig.ContentSettings.DefaultLangCode
            };
            return View(vm);
        }

        [HttpPost("content")]
        public async Task<IActionResult> Content(ContentSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

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

        #region Email Settings

        [HttpGet("notification")]
        public IActionResult Notification()
        {
            var settings = _blogConfig.NotificationSettings;
            var vm = new NotificationSettingsViewModel
            {
                EmailDisplayName = settings.EmailDisplayName,
                EnableEmailSending = settings.EnableEmailSending,
                SendEmailOnCommentReply = settings.SendEmailOnCommentReply,
                SendEmailOnNewComment = settings.SendEmailOnNewComment
            };
            return View(vm);
        }

        [HttpPost("notification")]
        public async Task<IActionResult> Notification(NotificationSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.NotificationSettings;
            settings.EmailDisplayName = model.EmailDisplayName;
            settings.EnableEmailSending = model.EnableEmailSending;
            settings.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
            settings.SendEmailOnNewComment = model.SendEmailOnNewComment;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedNotification, "Notification Settings updated.");

            return Ok();
        }

        [HttpPost("send-test-email")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendTestEmail([FromServices] IBlogNotificationClient notificationClient)
        {
            await notificationClient.TestNotificationAsync();
            return Json(true);
        }

        #endregion

        #region Feed Settings

        [HttpGet("subscription")]
        public IActionResult Subscription()
        {
            var settings = _blogConfig.FeedSettings;
            var vm = new SubscriptionSettingsViewModel
            {
                AuthorName = settings.AuthorName,
                RssCopyright = settings.RssCopyright,
                RssDescription = settings.RssDescription,
                RssItemCount = settings.RssItemCount,
                RssTitle = settings.RssTitle,
                UseFullContent = settings.UseFullContent
            };

            return View(vm);
        }

        [HttpPost("subscription")]
        public async Task<IActionResult> Subscription(SubscriptionSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

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

        #endregion

        #region Watermark Settings

        [HttpGet("watermark")]
        public IActionResult Watermark()
        {
            var settings = _blogConfig.WatermarkSettings;
            var vm = new WatermarkSettingsViewModel
            {
                IsEnabled = settings.IsEnabled,
                KeepOriginImage = settings.KeepOriginImage,
                FontSize = settings.FontSize,
                WatermarkText = settings.WatermarkText
            };

            return View(vm);
        }

        [HttpPost("watermark")]
        public async Task<IActionResult> Watermark(WatermarkSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.WatermarkSettings;
            settings.IsEnabled = model.IsEnabled;
            settings.KeepOriginImage = model.KeepOriginImage;
            settings.FontSize = model.FontSize;
            settings.WatermarkText = model.WatermarkText;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedWatermark, "Watermark Settings updated.");

            return Ok();
        }

        #endregion


        [HttpPost("friendlink")]
        public async Task<IActionResult> FriendLink(FriendLinkSettingsViewModelWrap model)
        {
            var fs = _blogConfig.FriendLinksSettings;
            fs.ShowFriendLinksSection = model.FriendLinkSettingsViewModel.ShowFriendLinksSection;

            await _blogConfig.SaveAsync(fs);
            return Ok();
        }


        #region User Avatar

        [HttpPost("set-blogger-avatar")]
        [TypeFilter(typeof(ClearBlogCache), Arguments = new object[] { CacheDivision.General, "avatar" })]
        public async Task<IActionResult> SetBloggerAvatar(string base64Img)
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

                return Json(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading avatar image.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Site Icon

        [HttpPost("set-siteicon")]
        public async Task<IActionResult> SetSiteIcon(string base64Img)
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

                if (Directory.Exists(SiteIconDirectory))
                {
                    Directory.Delete(SiteIconDirectory, true);
                }

                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Site icon updated.");

                return Json(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading avatar image.");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        #endregion

        #region Advanced Settings

        [HttpGet("advanced")]
        public IActionResult Advanced()
        {
            var settings = _blogConfig.AdvancedSettings;
            var vm = new AdvancedSettingsViewModel
            {
                DNSPrefetchEndpoint = settings.DNSPrefetchEndpoint,
                RobotsTxtContent = settings.RobotsTxtContent,
                EnablePingbackSend = settings.EnablePingBackSend,
                EnablePingbackReceive = settings.EnablePingBackReceive,
                EnableOpenGraph = settings.EnableOpenGraph,
                EnableCDNRedirect = settings.EnableCDNRedirect,
                CDNEndpoint = settings.CDNEndpoint
            };

            return View(vm);
        }

        [HttpPost("advanced")]
        public async Task<IActionResult> Advanced(AdvancedSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.AdvancedSettings;
            settings.DNSPrefetchEndpoint = model.DNSPrefetchEndpoint;
            settings.RobotsTxtContent = model.RobotsTxtContent;
            settings.EnablePingBackSend = model.EnablePingbackSend;
            settings.EnablePingBackReceive = model.EnablePingbackReceive;
            settings.EnableOpenGraph = model.EnableOpenGraph;
            settings.EnableCDNRedirect = model.EnableCDNRedirect;

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
        public IActionResult Shutdown([FromServices] IHostApplicationLifetime applicationLifetime)
        {
            _logger.LogWarning($"Shutdown is requested by '{User.Identity?.Name}'.");
            applicationLifetime.StopApplication();
            return Accepted();
        }

        [HttpPost("reset")]
        public async Task<IActionResult> Reset([FromServices] IDbConnection dbConnection, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            _logger.LogWarning($"System reset is requested by '{User.Identity?.Name}', IP: {HttpContext.Connection.RemoteIpAddress}.");

            var setupHelper = new SetupRunner(dbConnection);
            setupHelper.ClearData();

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "System reset.");

            applicationLifetime.StopApplication();
            return Accepted();
        }

        #endregion

        #region Security Settings

        [HttpGet("security")]
        public IActionResult Security()
        {
            var settings = _blogConfig.SecuritySettings;
            var vm = new SecuritySettingsViewModel
            {
                WarnExternalLink = settings.WarnExternalLink,
                AllowScriptsInPage = settings.AllowScriptsInPage,
                ShowAdminLoginButton = settings.ShowAdminLoginButton
            };

            return View(vm);
        }

        [HttpPost("security")]
        public async Task<IActionResult> Security(SecuritySettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.SecuritySettings;
            settings.WarnExternalLink = model.WarnExternalLink;
            settings.AllowScriptsInPage = model.AllowScriptsInPage;
            settings.ShowAdminLoginButton = model.ShowAdminLoginButton;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Security Settings updated.");
            return Ok();
        }

        #endregion

        #region CustomCss

        [HttpGet("custom-css")]
        public IActionResult CustomStyleSheet()
        {
            var settings = _blogConfig.CustomStyleSheetSettings;
            var vm = new CustomStyleSheetSettingsViewModel
            {
                EnableCustomCss = settings.EnableCustomCss,
                CssCode = settings.CssCode
            };

            return View(vm);
        }

        [HttpPost("custom-css")]
        public async Task<IActionResult> CustomStyleSheet(CustomStyleSheetSettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.CustomStyleSheetSettings;

            if (model.EnableCustomCss && string.IsNullOrWhiteSpace(model.CssCode))
            {
                ModelState.AddModelError(nameof(CustomStyleSheetSettingsViewModel.CssCode), "CSS Code is required");
                return BadRequest(ModelState);
            }

            var uglifyTest = Uglify.Css(model.CssCode);
            if (uglifyTest.HasErrors)
            {
                foreach (var err in uglifyTest.Errors)
                {
                    ModelState.AddModelError(model.CssCode, err.ToString());
                }
                return BadRequest(ModelState);
            }

            settings.EnableCustomCss = model.EnableCustomCss;
            settings.CssCode = model.CssCode;

            await _blogConfig.SaveAsync(settings);
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Custom Style Sheet Settings updated.");
            return Ok();
        }

        #endregion

        #region DataPorting

        [HttpGet("data-porting")]
        public IActionResult DataPorting()
        {
            return View();
        }

        [HttpGet("export/{type}")]
        public async Task<IActionResult> ExportDownload([FromServices] IExportManager expman, ExportDataType type)
        {
            var exportResult = await expman.ExportData(type);
            switch (exportResult.ExportFormat)
            {
                case ExportFormat.SingleJsonFile:
                    return new FileContentResult(exportResult.Content, exportResult.ContentType)
                    {
                        FileDownloadName = $"moonglade-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json"
                    };
                case ExportFormat.SingleCSVFile:
                    Response.Headers.Add("Content-Disposition", $"attachment;filename={Path.GetFileName(exportResult.FilePath)}");
                    return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));
                case ExportFormat.ZippedJsonFiles:
                    return PhysicalFile(exportResult.FilePath, exportResult.ContentType, Path.GetFileName(exportResult.FilePath));
                default:
                    return BadRequest(ModelState);
            }
        }

        #endregion

        [HttpPost("clear-data-cache")]
        public IActionResult ClearDataCache(string[] cachedObjectValues, [FromServices] IBlogCache cache)
        {
            static void DeleteIfExists(string path)
            {
                if (Directory.Exists(path)) Directory.Delete(path);
            }

            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                if (cachedObjectValues.Contains("MCO_IMEM"))
                {
                    cache.RemoveAllCache();
                }

                if (cachedObjectValues.Contains("MCO_SICO"))
                {
                    DeleteIfExists(SiteIconDirectory);
                }

                return Ok();

            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}