using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Core.Notification;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Setup;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin/settings")]
    public class SettingsController : MoongladeController
    {
        #region Private Fields

        private readonly FriendLinkService _friendLinkService;
        private readonly IBlogConfig _blogConfig;

        #endregion

        public SettingsController(
            ILogger<SettingsController> logger,
            IOptionsSnapshot<AppSettings> settings,
            FriendLinkService friendLinkService,
            IBlogConfig blogConfig)
            : base(logger, settings)
        {
            _blogConfig = blogConfig;

            _friendLinkService = friendLinkService;
        }

        [HttpGet("general-settings")]
        public IActionResult GeneralSettings()
        {
            var tzList = Utils.GetTimeZones().Select(t => new SelectListItem
            {
                Text = t.DisplayName,
                Value = t.Id
            }).ToList();

            var tmList = Utils.GetThemes().Select(t => new SelectListItem
            {
                Text = t.Key,
                Value = t.Value
            }).ToList();

            var vm = new GeneralSettingsViewModel
            {
                LogoText = _blogConfig.GeneralSettings.LogoText,
                MetaKeyword = _blogConfig.GeneralSettings.MetaKeyword,
                MetaDescription = _blogConfig.GeneralSettings.MetaDescription,
                SiteTitle = _blogConfig.GeneralSettings.SiteTitle,
                Copyright = _blogConfig.GeneralSettings.Copyright,
                SideBarCustomizedHtmlPitch = _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch,
                FooterCustomizedHtmlPitch = _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch,
                BloggerName = _blogConfig.BlogOwnerSettings.Name,
                BloggerDescription = _blogConfig.BlogOwnerSettings.Description,
                BloggerShortDescription = _blogConfig.BlogOwnerSettings.ShortDescription,
                SelectedTimeZoneId = _blogConfig.GeneralSettings.TimeZoneId,
                SelectedUtcOffset = Utils.GetTimeSpanByZoneId(_blogConfig.GeneralSettings.TimeZoneId),
                TimeZoneList = tzList,
                SelectedThemeFileName = _blogConfig.GeneralSettings.ThemeFileName,
                ThemeList = tmList
            };
            return View(vm);
        }

        [HttpPost("general-settings")]
        public async Task<IActionResult> GeneralSettings(GeneralSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _blogConfig.GeneralSettings.MetaKeyword = model.MetaKeyword;
                _blogConfig.GeneralSettings.MetaDescription = model.MetaDescription;
                _blogConfig.GeneralSettings.SiteTitle = model.SiteTitle;
                _blogConfig.GeneralSettings.Copyright = model.Copyright;
                _blogConfig.GeneralSettings.LogoText = model.LogoText;
                _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
                _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
                _blogConfig.GeneralSettings.TimeZoneUtcOffset = Utils.GetTimeSpanByZoneId(model.SelectedTimeZoneId).ToString();
                _blogConfig.GeneralSettings.TimeZoneId = model.SelectedTimeZoneId;
                _blogConfig.GeneralSettings.ThemeFileName = model.SelectedThemeFileName;
                await _blogConfig.SaveConfigurationAsync(_blogConfig.GeneralSettings);

                _blogConfig.BlogOwnerSettings.Name = model.BloggerName;
                _blogConfig.BlogOwnerSettings.Description = model.BloggerDescription;
                _blogConfig.BlogOwnerSettings.ShortDescription = model.BloggerShortDescription;
                var response = await _blogConfig.SaveConfigurationAsync(_blogConfig.BlogOwnerSettings);

                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated GeneralSettings");
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        [HttpGet("content-settings")]
        public IActionResult ContentSettings()
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
                EnableImageLazyLoad = _blogConfig.ContentSettings.EnableImageLazyLoad,
                ShowCalloutSection = _blogConfig.ContentSettings.ShowCalloutSection,
                CalloutSectionHtmlPitch = _blogConfig.ContentSettings.CalloutSectionHtmlPitch,
                ShowPostFooter = _blogConfig.ContentSettings.ShowPostFooter,
                PostFooterHtmlPitch = _blogConfig.ContentSettings.PostFooterHtmlPitch
            };
            return View(vm);
        }

        [HttpPost("content-settings")]
        public async Task<IActionResult> ContentSettings(ContentSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _blogConfig.ContentSettings.DisharmonyWords = model.DisharmonyWords;
                _blogConfig.ContentSettings.EnableComments = model.EnableComments;
                _blogConfig.ContentSettings.RequireCommentReview = model.RequireCommentReview;
                _blogConfig.ContentSettings.EnableWordFilter = model.EnableWordFilter;
                _blogConfig.ContentSettings.UseFriendlyNotFoundImage = model.UseFriendlyNotFoundImage;
                _blogConfig.ContentSettings.PostListPageSize = model.PostListPageSize;
                _blogConfig.ContentSettings.HotTagAmount = model.HotTagAmount;
                _blogConfig.ContentSettings.EnableGravatar = model.EnableGravatar;
                _blogConfig.ContentSettings.EnableImageLazyLoad = model.EnableImageLazyLoad;
                _blogConfig.ContentSettings.ShowCalloutSection = model.ShowCalloutSection;
                _blogConfig.ContentSettings.CalloutSectionHtmlPitch = model.CalloutSectionHtmlPitch;
                _blogConfig.ContentSettings.ShowPostFooter = model.ShowPostFooter;
                _blogConfig.ContentSettings.PostFooterHtmlPitch = model.PostFooterHtmlPitch;
                var response = await _blogConfig.SaveConfigurationAsync(_blogConfig.ContentSettings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated ContentSettings");
                return Json(response);

            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #region Email Settings

        [HttpGet("email-settings")]
        public IActionResult EmailSettings()
        {
            var settings = _blogConfig.EmailSettings;
            var vm = new EmailSettingsViewModel
            {
                AdminEmail = settings.AdminEmail,
                BannedMailDomain = settings.BannedMailDomain,
                EmailDisplayName = settings.EmailDisplayName,
                EnableEmailSending = settings.EnableEmailSending,
                SendEmailOnCommentReply = settings.SendEmailOnCommentReply,
                SendEmailOnNewComment = settings.SendEmailOnNewComment
            };
            return View(vm);
        }

        [HttpPost("email-settings")]
        public async Task<IActionResult> EmailSettings(EmailSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = _blogConfig.EmailSettings;
                settings.AdminEmail = model.AdminEmail;
                settings.BannedMailDomain = model.BannedMailDomain;
                settings.EmailDisplayName = model.EmailDisplayName;
                settings.EnableEmailSending = model.EnableEmailSending;
                settings.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
                settings.SendEmailOnNewComment = model.SendEmailOnNewComment;

                var response = await _blogConfig.SaveConfigurationAsync(settings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated EmailSettings");
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        [HttpPost("send-test-email")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendTestEmail([FromServices] IMoongladeNotificationClient notificationClient)
        {
            var response = await notificationClient.SendTestNotificationAsync();
            if (!response.IsSuccess)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;
            }
            return Json(response);
        }

        #endregion

        #region Feed Settings

        [HttpGet("feed-settings")]
        public IActionResult FeedSettings()
        {
            var settings = _blogConfig.FeedSettings;
            var vm = new FeedSettingsViewModel
            {
                AuthorName = settings.AuthorName,
                RssCopyright = settings.RssCopyright,
                RssDescription = settings.RssDescription,
                RssGeneratorName = settings.RssGeneratorName,
                RssItemCount = settings.RssItemCount,
                RssTitle = settings.RssTitle
            };

            return View(vm);
        }

        [HttpPost("feed-settings")]
        public async Task<IActionResult> FeedSettings(FeedSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = _blogConfig.FeedSettings;
                settings.AuthorName = model.AuthorName;
                settings.RssCopyright = model.RssCopyright;
                settings.RssDescription = model.RssDescription;
                settings.RssGeneratorName = model.RssGeneratorName;
                settings.RssItemCount = model.RssItemCount;
                settings.RssTitle = model.RssTitle;

                var response = await _blogConfig.SaveConfigurationAsync(settings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated FeedSettings");
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #endregion

        #region Watermark Settings

        [HttpGet("watermark-settings")]
        public IActionResult WatermarkSettings()
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

        [HttpPost("watermark-settings")]
        public async Task<IActionResult> WatermarkSettings(WatermarkSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = _blogConfig.WatermarkSettings;
                settings.IsEnabled = model.IsEnabled;
                settings.KeepOriginImage = model.KeepOriginImage;
                settings.FontSize = model.FontSize;
                settings.WatermarkText = model.WatermarkText;

                var response = await _blogConfig.SaveConfigurationAsync(settings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated WatermarkSettings");
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #endregion

        #region FriendLinks

        [HttpPost("friendlink-settings")]
        public async Task<IActionResult> FriendLinkSettings(FriendLinkSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var fs = _blogConfig.FriendLinksSettings;
                fs.ShowFriendLinksSection = model.ShowFriendLinksSection;

                var response = await _blogConfig.SaveConfigurationAsync(fs);
                _blogConfig.RequireRefresh();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }


        [HttpGet("manage-friendlinks")]
        public async Task<IActionResult> ManageFriendLinks()
        {
            var response = await _friendLinkService.GetAllFriendLinksAsync();
            if (response.IsSuccess)
            {
                var vm = new FriendLinkSettingsViewModelWrap
                {
                    FriendLinkSettingsViewModel = new FriendLinkSettingsViewModel
                    {
                        ShowFriendLinksSection = _blogConfig.FriendLinksSettings.ShowFriendLinksSection
                    },
                    FriendLinks = response.Item
                };

                return View(vm);
            }

            SetFriendlyErrorMessage();
            return View();
        }

        [HttpGet("create-friendlink")]
        public IActionResult CreateFriendLink()
        {
            return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
        }

        [HttpPost("create-friendlink")]
        public async Task<IActionResult> CreateFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var response = await _friendLinkService.AddFriendLinkAsync(viewModel.Title, viewModel.LinkUrl);
                    if (response.IsSuccess)
                    {
                        Logger.LogInformation($"User '{User.Identity.Name}' created new friendlink '{viewModel.Title}' to '{viewModel.LinkUrl}'");
                        return RedirectToAction(nameof(ManageFriendLinks));
                    }
                    ModelState.AddModelError(string.Empty, response.Message);
                }
                return View("CreateOrEditFriendLink", viewModel);
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return View("CreateOrEditFriendLink", viewModel);
            }
        }

        [HttpGet("edit-friendlink")]
        public async Task<IActionResult> EditFriendLink(Guid id)
        {
            try
            {
                var response = await _friendLinkService.GetFriendLinkAsync(id);
                if (response.IsSuccess)
                {
                    return View("CreateOrEditFriendLink", new FriendLinkEditViewModel
                    {
                        Id = response.Item.Id,
                        LinkUrl = response.Item.LinkUrl,
                        Title = response.Item.Title
                    });
                }
                ModelState.AddModelError(string.Empty, response.Message);
                return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
            }
        }

        [HttpPost("edit-friendlink")]
        public async Task<IActionResult> EditFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                var response = await _friendLinkService.UpdateFriendLinkAsync(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
                if (response.IsSuccess)
                {
                    Logger.LogInformation($"User '{User.Identity.Name}' updated friendlink id: '{viewModel.Id}'");

                    return RedirectToAction(nameof(ManageFriendLinks));
                }
                ModelState.AddModelError(string.Empty, response.Message);
                return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
            }
            catch (Exception e)
            {
                ModelState.AddModelError(string.Empty, e.Message);
                return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
            }
        }

        [HttpGet("delete-friendlink")]
        public async Task<IActionResult> DeleteFriendLink(Guid id)
        {
            var response = await _friendLinkService.DeleteFriendLinkAsync(id);
            Logger.LogInformation($"User '{User.Identity.Name}' deleting friendlink id: '{id}'");

            return response.IsSuccess ? RedirectToAction(nameof(ManageFriendLinks)) : ServerError();
        }

        #endregion

        #region User Avatar

        [HttpPost("set-blogger-avatar")]
        [TypeFilter(typeof(DeleteMemoryCache), Arguments = new object[] { StaticCacheKeys.Avatar })]
        public async Task<IActionResult> SetBloggerAvatar(string base64Avatar)
        {
            try
            {
                base64Avatar = base64Avatar.Trim();
                if (!Utils.TryParseBase64(base64Avatar, out var base64Chars))
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
                    Logger.LogError("Invalid base64Avatar Image", e);
                    return BadRequest();
                }

                _blogConfig.BlogOwnerSettings.AvatarBase64 = base64Avatar;
                var response = await _blogConfig.SaveConfigurationAsync(_blogConfig.BlogOwnerSettings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated avatar.");
                return Json(response);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading avatar image.");
                return ServerError();
            }
        }

        #endregion

        #region Advanced Settings

        [HttpGet("advanced-settings")]
        public IActionResult AdvancedSettings()
        {
            var settings = _blogConfig.AdvancedSettings;
            var vm = new AdvancedSettingsViewModel
            {
                DNSPrefetchEndpoint = settings.DNSPrefetchEndpoint,
                EnablePingBackSend = settings.EnablePingBackSend,
                EnablePingBackReceive = settings.EnablePingBackReceive
            };

            return View(vm);
        }

        [HttpPost("advanced-settings")]
        public async Task<IActionResult> AdvancedSettings(AdvancedSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = _blogConfig.AdvancedSettings;
                settings.DNSPrefetchEndpoint = model.DNSPrefetchEndpoint;
                settings.EnablePingBackSend = model.EnablePingBackSend;
                settings.EnablePingBackReceive = model.EnablePingBackReceive;

                var response = await _blogConfig.SaveConfigurationAsync(settings);
                _blogConfig.RequireRefresh();

                Logger.LogInformation($"User '{User.Identity.Name}' updated AdvancedSettings");
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        [HttpPost("shutdown")]
        public IActionResult Shutdown(int nonce, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"Shutdown is requested by '{User.Identity.Name}'. Nonce value: {nonce}");
            applicationLifetime.StopApplication();
            return Ok();
        }

        [HttpPost("reset")]
        public IActionResult Reset(int nonce, [FromServices] IConfiguration configuration, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"System reset is requested by '{User.Identity.Name}', IP: {HttpContext.Connection.RemoteIpAddress}. Nonce value: {nonce}");
            var conn = configuration.GetConnectionString(Constants.DbConnectionName);
            var setupHelper = new SetupHelper(conn);
            var response = setupHelper.ClearData();
            if (!response.IsSuccess) return ServerError(response.Message);
            applicationLifetime.StopApplication();
            return Ok();
        }

        #endregion

        public IActionResult About()
        {
            return View();
        }
    }
}