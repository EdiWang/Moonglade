using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.DataPorting;
using Moonglade.DateTimeOps;
using Moonglade.Model.Settings;
using Moonglade.Setup;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using NUglify;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin/settings")]
    public class SettingsController : BlogController
    {
        #region Private Fields

        private readonly IFriendLinkService _friendLinkService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<SettingsController> _logger;

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
                SideBarOption = _blogConfig.GeneralSettings.SideBarOption.ToString(),
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

        [HttpPost("general")]
        public async Task<IActionResult> General(GeneralSettingsViewModel model, [FromServices] IDateTimeResolver dateTimeResolver)
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
            _blogConfig.GeneralSettings.TimeZoneUtcOffset = dateTimeResolver.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
            _blogConfig.GeneralSettings.TimeZoneId = model.SelectedTimeZoneId;
            _blogConfig.GeneralSettings.ThemeFileName = model.SelectedThemeFileName;
            _blogConfig.GeneralSettings.OwnerName = model.OwnerName;
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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.NotificationSettings;
            settings.AdminEmail = model.AdminEmail;
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

        #region FriendLinks

        [HttpGet("friendlink")]
        public async Task<IActionResult> FriendLink()
        {
            var links = await _friendLinkService.GetAllAsync();
            var vm = new FriendLinkSettingsViewModelWrap
            {
                FriendLinkSettingsViewModel = new()
                {
                    ShowFriendLinksSection = _blogConfig.FriendLinksSettings.ShowFriendLinksSection
                },
                FriendLinks = links
            };

            return View(vm);
        }

        [HttpPost("friendlink")]
        public async Task<IActionResult> FriendLink(FriendLinkSettingsViewModelWrap model)
        {
            //if (!ModelState.IsValid) return BadRequest(ModelState);

            var fs = _blogConfig.FriendLinksSettings;
            fs.ShowFriendLinksSection = model.FriendLinkSettingsViewModel.ShowFriendLinksSection;

            await _blogConfig.SaveAsync(fs);
            return Ok();
        }

        [HttpPost("friendlink/create")]
        public async Task<IActionResult> CreateFriendLink([FromBody] FriendLinkEditViewModel viewModel)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _friendLinkService.AddAsync(viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpGet("friendlink/edit/{id:guid}")]
        public async Task<IActionResult> EditFriendLink(Guid id)
        {
            try
            {
                var link = await _friendLinkService.GetAsync(id);
                if (null == link) return NotFound();

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
        public async Task<IActionResult> EditFriendLink([FromBody] FriendLinkEditViewModel viewModel)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            await _friendLinkService.UpdateAsync(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
            return Ok(viewModel);
        }

        [HttpDelete("friendlink/{id:guid}")]
        public async Task<IActionResult> DeleteFriendLink(Guid id)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState);
            }

            await _friendLinkService.DeleteAsync(id);
            return Ok();
        }

        #endregion

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
                    return ServerError(e.Message);
                }

                _blogConfig.GeneralSettings.AvatarBase64 = base64Img;
                await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);
                await _blogAudit.AddAuditEntry(EventType.Settings, AuditEventId.SettingsSavedGeneral, "Avatar updated.");

                return Json(true);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error uploading avatar image.");
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
                    return ServerError(e.Message);
                }

                _blogConfig.GeneralSettings.SiteIconBase64 = base64Img;
                await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);

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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var settings = _blogConfig.AdvancedSettings;
            settings.DNSPrefetchEndpoint = model.DNSPrefetchEndpoint;
            settings.RobotsTxtContent = model.RobotsTxtContent;
            settings.EnablePingBackSend = model.EnablePingbackSend;
            settings.EnablePingBackReceive = model.EnablePingbackReceive;
            settings.EnableOpenGraph = model.EnableOpenGraph;

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
                ShowAdminLoginButton = settings.ShowAdminLoginButton,
                EnablePostRawEndpoint = settings.EnablePostRawEndpoint
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
            settings.EnablePostRawEndpoint = model.EnablePostRawEndpoint;

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

        #region Audit Logs

        [HttpGet("auditlogs")]
        public async Task<IActionResult> AuditLogs([FromServices] IFeatureManager featureManager, int page = 1)
        {
            var flag = await featureManager.IsEnabledAsync(nameof(FeatureFlags.EnableAudit));
            if (!flag) return View();

            if (page < 0) return BadRequest(ModelState);

            var skip = (page - 1) * 20;

            var (entries, count) = await _blogAudit.GetAuditEntries(skip, 20);
            var list = new StaticPagedList<AuditEntry>(entries, page, 20, count);

            return View(list);
        }

        [HttpGet("clear-auditlogs")]
        [FeatureGate(FeatureFlags.EnableAudit)]
        public async Task<IActionResult> ClearAuditLogs()
        {
            await _blogAudit.ClearAuditLog();
            return RedirectToAction("AuditLogs");
        }

        #endregion

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
                return ServerError(e.Message);
            }
        }

        #region Account

        [HttpGet("account")]
        public async Task<IActionResult> AccountSettings([FromServices] ILocalAccountService accountService)
        {
            var accounts = await accountService.GetAllAsync();
            var vm = new AccountManageViewModel { Accounts = accounts };

            return View(vm);
        }

        [HttpPost("account/create")]
        public async Task<IActionResult> CreateAccount([FromBody] AccountEditViewModel model, [FromServices] ILocalAccountService accountService)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (accountService.Exist(model.Username))
            {
                ModelState.AddModelError("username", $"User '{model.Username}' already exist.");
                return Conflict(ModelState);
            }

            await accountService.CreateAsync(model.Username, model.Password);
            return Ok();
        }

        [HttpDelete("account/{id:guid}")]
        public async Task<IActionResult> DeleteAccount(Guid id, [FromServices] ILocalAccountService accountService)
        {
            if (id == Guid.Empty)
            {
                ModelState.AddModelError(nameof(id), "value is empty");
                return BadRequest(ModelState);
            }

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
            return Ok();
        }

        [HttpPost("account/{id:guid}/reset-password")]
        public async Task<IActionResult> ResetAccountPassword(
            Guid id, [FromBody] ResetPasswordRequest request, [FromServices] ILocalAccountService accountService)
        {
            if (id == Guid.Empty) ModelState.AddModelError(nameof(id), "value is empty");
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!Regex.IsMatch(request.NewPassword, @"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$"))
            {
                return Conflict("Password must be minimum eight characters, at least one letter and one number");
            }

            await accountService.UpdatePasswordAsync(id, request.NewPassword);
            return Ok();
        }

        #endregion
    }

    public class ResetPasswordRequest
    {
        [Required]
        public string NewPassword { get; set; }
    }
}