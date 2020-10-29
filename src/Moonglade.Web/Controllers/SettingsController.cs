using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.DataPorting;
using Moonglade.DateTimeOps;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Setup;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin/settings")]
    public class SettingsController : BlogController
    {
        #region Private Fields

        private readonly FriendLinkService _friendLinkService;
        private readonly AppSettings _settings;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;

        #endregion

        public SettingsController(
            ILogger<SettingsController> logger,
            IOptionsSnapshot<AppSettings> settings,
            FriendLinkService friendLinkService,
            IBlogConfig blogConfig,
            IBlogAudit blogAudit)
            : base(logger)
        {
            _settings = settings.Value;
            _blogConfig = blogConfig;
            _blogAudit = blogAudit;

            _friendLinkService = friendLinkService;
        }

        [AllowAnonymous]
        [HttpGet("set-lang")]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return LocalRedirect(returnUrl);
        }

        [HttpGet("general-settings")]
        public IActionResult General([FromServices] IDateTimeResolver dateTimeResolver)
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
                FooterCustomizedHtmlPitch = _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch,
                OwnerName = _blogConfig.GeneralSettings.OwnerName,
                OwnerDescription = _blogConfig.GeneralSettings.Description,
                OwnerShortDescription = _blogConfig.GeneralSettings.ShortDescription,
                SelectedTimeZoneId = _blogConfig.GeneralSettings.TimeZoneId,
                SelectedUtcOffset = dateTimeResolver.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId),
                SelectedThemeFileName = _blogConfig.GeneralSettings.ThemeFileName,
                AutoDarkLightTheme = _blogConfig.GeneralSettings.AutoDarkLightTheme
            };
            return View(vm);
        }

        [HttpPost("general-settings")]
        public async Task<IActionResult> General(GeneralSettingsViewModel model, [FromServices] IDateTimeResolver dateTimeResolver)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            _blogConfig.GeneralSettings.MetaKeyword = model.MetaKeyword;
            _blogConfig.GeneralSettings.MetaDescription = model.MetaDescription;
            _blogConfig.GeneralSettings.CanonicalPrefix = model.CanonicalPrefix;
            _blogConfig.GeneralSettings.SiteTitle = model.SiteTitle;
            _blogConfig.GeneralSettings.Copyright = model.Copyright;
            _blogConfig.GeneralSettings.LogoText = model.LogoText;
            _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
            _blogConfig.GeneralSettings.TimeZoneUtcOffset = dateTimeResolver.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
            _blogConfig.GeneralSettings.TimeZoneId = model.SelectedTimeZoneId;
            _blogConfig.GeneralSettings.ThemeFileName = model.SelectedThemeFileName;
            _blogConfig.GeneralSettings.OwnerName = model.OwnerName;
            _blogConfig.GeneralSettings.Description = model.OwnerDescription;
            _blogConfig.GeneralSettings.ShortDescription = model.OwnerShortDescription;
            _blogConfig.GeneralSettings.AutoDarkLightTheme = model.AutoDarkLightTheme;

            await _blogConfig.SaveConfigurationAsync(_blogConfig.GeneralSettings);
            _blogConfig.RequireRefresh();

            AppDomain.CurrentDomain.SetData("CurrentThemeColor", null);

            Logger.LogInformation($"User '{User.Identity.Name}' updated GeneralSettings");
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
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            _blogConfig.ContentSettings.DisharmonyWords = model.DisharmonyWords;
            _blogConfig.ContentSettings.EnableComments = model.EnableComments;
            _blogConfig.ContentSettings.RequireCommentReview = model.RequireCommentReview;
            _blogConfig.ContentSettings.EnableWordFilter = model.EnableWordFilter;
            _blogConfig.ContentSettings.UseFriendlyNotFoundImage = model.UseFriendlyNotFoundImage;
            _blogConfig.ContentSettings.PostListPageSize = model.PostListPageSize;
            _blogConfig.ContentSettings.HotTagAmount = model.HotTagAmount;
            _blogConfig.ContentSettings.EnableGravatar = model.EnableGravatar;
            _blogConfig.ContentSettings.ShowCalloutSection = model.ShowCalloutSection;
            _blogConfig.ContentSettings.CalloutSectionHtmlPitch = model.CalloutSectionHtmlPitch;
            _blogConfig.ContentSettings.ShowPostFooter = model.ShowPostFooter;
            _blogConfig.ContentSettings.PostFooterHtmlPitch = model.PostFooterHtmlPitch;
            _blogConfig.ContentSettings.DefaultLangCode = model.DefaultLangCode;

            await _blogConfig.SaveConfigurationAsync(_blogConfig.ContentSettings);
            _blogConfig.RequireRefresh();

            Logger.LogInformation($"User '{User.Identity.Name}' updated ContentSettings");
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
                AdminEmail = settings.AdminEmail,
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
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var settings = _blogConfig.NotificationSettings;
            settings.AdminEmail = model.AdminEmail;
            settings.EmailDisplayName = model.EmailDisplayName;
            settings.EnableEmailSending = model.EnableEmailSending;
            settings.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
            settings.SendEmailOnNewComment = model.SendEmailOnNewComment;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

            Logger.LogInformation($"User '{User.Identity.Name}' updated EmailSettings");
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
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var settings = _blogConfig.FeedSettings;
            settings.AuthorName = model.AuthorName;
            settings.RssCopyright = model.RssCopyright;
            settings.RssDescription = model.RssDescription;
            settings.RssItemCount = model.RssItemCount;
            settings.RssTitle = model.RssTitle;
            settings.UseFullContent = model.UseFullContent;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

            Logger.LogInformation($"User '{User.Identity.Name}' updated FeedSettings");
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
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var settings = _blogConfig.WatermarkSettings;
            settings.IsEnabled = model.IsEnabled;
            settings.KeepOriginImage = model.KeepOriginImage;
            settings.FontSize = model.FontSize;
            settings.WatermarkText = model.WatermarkText;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

            Logger.LogInformation($"User '{User.Identity.Name}' updated WatermarkSettings");
            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedWatermark, "Watermark Settings updated.");

            return Ok();
        }

        #endregion

        #region FriendLinks

        [HttpGet("friendlink")]
        public async Task<IActionResult> FriendLink()
        {
            try
            {
                var links = await _friendLinkService.GetAllAsync();
                var vm = new FriendLinkSettingsViewModelWrap
                {
                    FriendLinkSettingsViewModel = new FriendLinkSettingsViewModel
                    {
                        ShowFriendLinksSection = _blogConfig.FriendLinksSettings.ShowFriendLinksSection
                    },
                    FriendLinks = links
                };

                return View(vm);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                SetFriendlyErrorMessage();
                return View();
            }
        }

        [HttpPost("friendlink")]
        public async Task<IActionResult> FriendLink(FriendLinkSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var fs = _blogConfig.FriendLinksSettings;
            fs.ShowFriendLinksSection = model.ShowFriendLinksSection;

            await _blogConfig.SaveConfigurationAsync(fs);
            _blogConfig.RequireRefresh();
            return Ok();
        }

        [HttpPost("friendlink/create")]
        public async Task<IActionResult> CreateFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest();

                await _friendLinkService.AddAsync(viewModel.Title, viewModel.LinkUrl);
                return Json(viewModel);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return ServerError();
            }
        }

        [HttpGet("friendlink/edit/{id:guid}")]
        public async Task<IActionResult> EditFriendLink(Guid id)
        {
            try
            {
                var link = await _friendLinkService.GetAsync(id);
                if (null == link) return BadRequest();

                var obj = new FriendLinkEditViewModel
                {
                    Id = link.Id,
                    LinkUrl = link.LinkUrl,
                    Title = link.Title
                };

                return Json(obj);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return ServerError();
            }
        }

        [HttpPost("friendlink/edit")]
        public async Task<IActionResult> EditFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                await _friendLinkService.UpdateAsync(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
                return Json(viewModel);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return ServerError();
            }
        }

        [HttpPost("friendlink/delete")]
        public async Task<IActionResult> DeleteFriendLink(Guid id)
        {
            await _friendLinkService.DeleteAsync(id);
            return Json(id);
        }

        #endregion

        #region User Avatar

        [HttpPost("set-blogger-avatar")]
        [TypeFilter(typeof(DeleteBlogCache), Arguments = new object[] { CacheDivision.General, "avatar" })]
        public async Task<IActionResult> SetBloggerAvatar(string base64Img)
        {
            try
            {
                base64Img = base64Img.Trim();
                if (!Utils.TryParseBase64(base64Img, out var base64Chars))
                {
                    Logger.LogWarning("Bad base64 is used when setting avatar.");
                    return BadRequest();
                }

                try
                {
                    using var bmp = new Bitmap(new MemoryStream(base64Chars));
                    if (bmp.Height != bmp.Width || bmp.Height + bmp.Width != 600)
                    {
                        Logger.LogWarning("Avatar size is not 300x300, rejecting request.");

                        // Normal uploaded avatar should be a 300x300 pixel image
                        return BadRequest();
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError("Invalid base64img Image", e);
                    return BadRequest();
                }

                _blogConfig.GeneralSettings.AvatarBase64 = base64Img;
                await _blogConfig.SaveConfigurationAsync(_blogConfig.GeneralSettings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated avatar.");
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Avatar updated.");

                return Json(true);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading avatar image.");
                return ServerError();
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
                if (!Utils.TryParseBase64(base64Img, out var base64Chars))
                {
                    Logger.LogWarning("Bad base64 is used when setting site icon.");
                    return BadRequest();
                }

                try
                {
                    using var bmp = new Bitmap(new MemoryStream(base64Chars));
                    if (bmp.Height != bmp.Width) return BadRequest();
                }
                catch (Exception e)
                {
                    Logger.LogError("Invalid base64img Image", e);
                    return BadRequest();
                }

                _blogConfig.GeneralSettings.SiteIconBase64 = base64Img;
                await _blogConfig.SaveConfigurationAsync(_blogConfig.GeneralSettings);
                _blogConfig.RequireRefresh();

                if (Directory.Exists(SiteIconDirectory))
                {
                    Directory.Delete(SiteIconDirectory, true);
                }

                Logger.LogInformation($"User '{User.Identity.Name}' updated site icon.");
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Site icon updated.");

                return Json(true);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading avatar image.");
                return ServerError();
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
                EnableOpenGraph = settings.EnableOpenGraph
            };

            return View(vm);
        }

        [HttpPost("advanced")]
        public async Task<IActionResult> Advanced(AdvancedSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var settings = _blogConfig.AdvancedSettings;
            settings.DNSPrefetchEndpoint = model.DNSPrefetchEndpoint;
            settings.RobotsTxtContent = model.RobotsTxtContent;
            settings.EnablePingBackSend = model.EnablePingbackSend;
            settings.EnablePingBackReceive = model.EnablePingbackReceive;
            settings.EnableOpenGraph = model.EnableOpenGraph;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Advanced Settings updated.");
            return Ok();
        }

        [HttpPost("shutdown")]
        public IActionResult Shutdown(int nonce, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"Shutdown is requested by '{User.Identity.Name}'. Nonce value: {nonce}");
            applicationLifetime.StopApplication();
            return Ok();
        }

        [HttpPost("reset")]
        public async Task<IActionResult> Reset(int nonce, [FromServices] IConfiguration configuration, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"System reset is requested by '{User.Identity.Name}', IP: {HttpContext.Connection.RemoteIpAddress}. Nonce value: {nonce}");

            var conn = configuration.GetConnectionString(Constants.DbConnectionName);
            var setupHelper = new SetupRunner(conn);
            setupHelper.ClearData();

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "System reset.");

            applicationLifetime.StopApplication();
            return Ok();
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
                ShowAdminLoginButton = settings.ShowAdminLoginButton,
                EnablePostRawEndpoint = settings.EnablePostRawEndpoint
            };

            return View(vm);
        }

        [HttpPost("security")]
        public async Task<IActionResult> Security(SecuritySettingsViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest();

            var settings = _blogConfig.SecuritySettings;
            settings.WarnExternalLink = model.WarnExternalLink;
            settings.AllowScriptsInPage = model.AllowScriptsInPage;
            settings.ShowAdminLoginButton = model.ShowAdminLoginButton;
            settings.EnablePostRawEndpoint = model.EnablePostRawEndpoint;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

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
            if (!ModelState.IsValid) return BadRequest();

            var settings = _blogConfig.CustomStyleSheetSettings;
            settings.EnableCustomCss = model.EnableCustomCss;
            settings.CssCode = model.CssCode;

            await _blogConfig.SaveConfigurationAsync(settings);
            _blogConfig.RequireRefresh();

            await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedAdvanced, "Security Settings updated.");
            return Ok();
        }

        #endregion

        #region Audit Logs

        [HttpGet("auditlogs")]
        public async Task<IActionResult> AuditLogs(int page = 1)
        {
            try
            {
                if (!_settings.EnableAudit)
                {
                    ViewBag.AuditLogDisabled = true;
                    return View();
                }

                if (page < 0) return BadRequest();

                var skip = (page - 1) * 20;

                var entries = await _blogAudit.GetAuditEntries(skip, 20);
                var list = new StaticPagedList<AuditEntry>(entries.Entries, page, 20, entries.Count);

                return View(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);

                SetFriendlyErrorMessage();
                return View();
            }
        }

        [HttpGet("clear-auditlogs")]
        public async Task<IActionResult> ClearAuditLogs()
        {
            try
            {
                if (!_settings.EnableAudit) return BadRequest();

                await _blogAudit.ClearAuditLog();
                return RedirectToAction("AuditLogs");
            }
            catch (Exception e)
            {
                return ServerError(e.Message);
            }
        }

        #endregion

        [HttpGet("settings-about")]
        public IActionResult About()
        {
            return View();
        }

        #region DataPorting

        [HttpGet("data-porting")]
        public IActionResult DataPorting()
        {
            return View();
        }

        [HttpGet("export/{type}")]
        public async Task<IActionResult> Export4Download([FromServices] IExportManager expman, ExportDataType type)
        {
            var exportResult = await expman.ExportData(type);
            switch (exportResult.ExportFormat)
            {
                case ExportFormat.SingleJsonFile:
                    var bytes = Encoding.UTF8.GetBytes(exportResult.JsonContent);

                    return new FileContentResult(bytes, "application/octet-stream")
                    {
                        FileDownloadName = $"moonglade-{type.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.json"
                    };
                case ExportFormat.ZippedJsonFiles:
                    return PhysicalFile(exportResult.ZipFilePath, "application/zip", Path.GetFileName(exportResult.ZipFilePath));
                default:
                    return BadRequest();
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
                if (!ModelState.IsValid) return Conflict(ModelState);

                if (cachedObjectValues.Contains("MCO_IMEM"))
                {
                    cache.RemoveAllCache();
                }

                if (cachedObjectValues.Contains("MCO_OPML"))
                {
                    var opmlDataFile = Path.Join($"{DataDirectory}", $"{Constants.OpmlFileName}");
                    DeleteIfExists(opmlDataFile);
                }

                if (cachedObjectValues.Contains("MCO_FEED"))
                {
                    var feedDir = Path.Join($"{DataDirectory}", "feed");
                    DeleteIfExists(feedDir);
                }

                if (cachedObjectValues.Contains("MCO_OPSH"))
                {
                    var openSearchDataFile = Path.Join($"{DataDirectory}", $"{Constants.OpenSearchFileName}");
                    DeleteIfExists(openSearchDataFile);
                }

                if (cachedObjectValues.Contains("MCO_SICO"))
                {
                    DeleteIfExists(SiteIconDirectory);
                }

                return Ok();

            }
            catch (Exception e)
            {
                return ServerError(e.Message);
            }
        }

        #region Account

        [HttpGet("account")]
        public async Task<IActionResult> AccountSettings([FromServices] LocalAccountService accountService)
        {
            var accounts = await accountService.GetAllAsync();
            var vm = new AccountManageViewModel { Accounts = accounts };

            return View(vm);
        }

        [HttpPost("account/create")]
        public async Task<IActionResult> CreateAccount(AccountEditViewModel model, [FromServices] LocalAccountService accountService)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid ModelState");
            if (accountService.Exist(model.Username))
            {
                ModelState.AddModelError("username", $"User '{model.Username}' already exist.");
                return Conflict(ModelState);
            }

            var uid = await accountService.CreateAsync(model.Username, model.Password);
            return Json(uid);
        }

        [HttpPost("account/delete")]
        public async Task<IActionResult> DeleteAccount(Guid id, [FromServices] LocalAccountService accountService)
        {
            var uidClaim = User.Claims.FirstOrDefault(c => c.Type == "uid");
            if (null == uidClaim || string.IsNullOrWhiteSpace(uidClaim.Value))
            {
                return ServerError("Can not get current uid.");
            }

            if (id.ToString() == uidClaim.Value)
            {
                return Conflict("Can not delete current user.");
            }

            var count = accountService.Count();
            if (count == 1)
            {
                return Conflict("Can not delete last account.");
            }

            await accountService.DeleteAsync(id);
            return Json(id);
        }

        [HttpPost("account/reset-password")]
        public async Task<IActionResult> ResetAccountPassword(Guid id, string newPassword, [FromServices] LocalAccountService accountService)
        {
            if (id == Guid.Empty)
            {
                return Conflict("Id can not be empty.");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return Conflict("newPassword can not be empty.");
            }

            if (!Regex.IsMatch(newPassword, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$"))
            {
                return Conflict("Password must be minimum eight characters, at least one letter and one number");
            }

            await accountService.UpdatePasswordAsync(id, newPassword);
            return Json(id);
        }

        #endregion
    }
}