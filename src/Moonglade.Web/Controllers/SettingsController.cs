using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            var vm = new GeneralSettingsViewModel
            {
                LogoText = _blogConfig.GeneralSettings.LogoText,
                MetaKeyword = _blogConfig.GeneralSettings.MetaKeyword,
                SiteTitle = _blogConfig.GeneralSettings.SiteTitle,
                Copyright = _blogConfig.GeneralSettings.Copyright,
                SideBarCustomizedHtmlPitch = _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch,
                FooterCustomizedHtmlPitch = _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch,
                ShowCalloutSection = _blogConfig.GeneralSettings.ShowCalloutSection,
                CalloutSectionHtmlPitch = _blogConfig.GeneralSettings.CalloutSectionHtmlPitch,
                BloggerName = _blogConfig.BlogOwnerSettings.Name,
                BloggerDescription = _blogConfig.BlogOwnerSettings.Description,
                BloggerShortDescription = _blogConfig.BlogOwnerSettings.ShortDescription
            };
            return View(vm);
        }

        [HttpPost("general-settings")]
        public async Task<IActionResult> GeneralSettings(GeneralSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _blogConfig.GeneralSettings.MetaKeyword = model.MetaKeyword;
                _blogConfig.GeneralSettings.SiteTitle = model.SiteTitle;
                _blogConfig.GeneralSettings.Copyright = model.Copyright;
                _blogConfig.GeneralSettings.LogoText = model.LogoText;
                _blogConfig.GeneralSettings.SideBarCustomizedHtmlPitch = model.SideBarCustomizedHtmlPitch;
                _blogConfig.GeneralSettings.FooterCustomizedHtmlPitch = model.FooterCustomizedHtmlPitch;
                _blogConfig.GeneralSettings.ShowCalloutSection = model.ShowCalloutSection;
                _blogConfig.GeneralSettings.CalloutSectionHtmlPitch = model.CalloutSectionHtmlPitch;
                await _blogConfig.SaveConfigurationAsync(_blogConfig.GeneralSettings);

                _blogConfig.BlogOwnerSettings.Name = model.BloggerName;
                _blogConfig.BlogOwnerSettings.Description = model.BloggerDescription;
                _blogConfig.BlogOwnerSettings.ShortDescription = model.BloggerShortDescription;
                var response = await _blogConfig.SaveConfigurationAsync(_blogConfig.BlogOwnerSettings);

                _blogConfig.RequireRefresh();
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
                EnableWordFilter = _blogConfig.ContentSettings.EnableWordFilter,
                UseFriendlyNotFoundImage = _blogConfig.ContentSettings.UseFriendlyNotFoundImage,
                PostListPageSize = _blogConfig.ContentSettings.PostListPageSize,
                HotTagAmount = _blogConfig.ContentSettings.HotTagAmount,
                EnableGravatar = _blogConfig.ContentSettings.EnableGravatar
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
                _blogConfig.ContentSettings.EnableWordFilter = model.EnableWordFilter;
                _blogConfig.ContentSettings.UseFriendlyNotFoundImage = model.UseFriendlyNotFoundImage;
                _blogConfig.ContentSettings.PostListPageSize = model.PostListPageSize;
                _blogConfig.ContentSettings.HotTagAmount = model.HotTagAmount;
                _blogConfig.ContentSettings.EnableGravatar = model.EnableGravatar;
                var response = await _blogConfig.SaveConfigurationAsync(_blogConfig.ContentSettings);
                _blogConfig.RequireRefresh();
                return Json(response);

            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #region Email Settings

        [HttpGet("email-settings")]
        public IActionResult EmailSettings()
        {
            var ec = _blogConfig.EmailSettings;
            var vm = new EmailSettingsViewModel
            {
                AdminEmail = ec.AdminEmail,
                BannedMailDomain = ec.BannedMailDomain,
                EmailDisplayName = ec.EmailDisplayName,
                EnableEmailSending = ec.EnableEmailSending,
                SendEmailOnCommentReply = ec.SendEmailOnCommentReply,
                SendEmailOnNewComment = ec.SendEmailOnNewComment
            };
            return View(vm);
        }

        [HttpPost("email-settings")]
        public async Task<IActionResult> EmailSettings(EmailSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ec = _blogConfig.EmailSettings;
                ec.AdminEmail = model.AdminEmail;
                ec.BannedMailDomain = model.BannedMailDomain;
                ec.EmailDisplayName = model.EmailDisplayName;
                ec.EnableEmailSending = model.EnableEmailSending;
                ec.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
                ec.SendEmailOnNewComment = model.SendEmailOnNewComment;

                var response = await _blogConfig.SaveConfigurationAsync(ec);
                _blogConfig.RequireRefresh();
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
            var fs = _blogConfig.FeedSettings;
            var vm = new FeedSettingsViewModel
            {
                AuthorName = fs.AuthorName,
                RssCopyright = fs.RssCopyright,
                RssDescription = fs.RssDescription,
                RssGeneratorName = fs.RssGeneratorName,
                RssItemCount = fs.RssItemCount,
                RssTitle = fs.RssTitle
            };

            return View(vm);
        }

        [HttpPost("feed-settings")]
        public async Task<IActionResult> FeedSettings(FeedSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var fs = _blogConfig.FeedSettings;
                fs.AuthorName = model.AuthorName;
                fs.RssCopyright = model.RssCopyright;
                fs.RssDescription = model.RssDescription;
                fs.RssGeneratorName = model.RssGeneratorName;
                fs.RssItemCount = model.RssItemCount;
                fs.RssTitle = model.RssTitle;

                var response = await _blogConfig.SaveConfigurationAsync(fs);
                _blogConfig.RequireRefresh();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #endregion

        #region Watermark Settings

        [HttpGet("watermark-settings")]
        public IActionResult WatermarkSettings()
        {
            var ws = _blogConfig.WatermarkSettings;
            var vm = new WatermarkSettingsViewModel
            {
                IsEnabled = ws.IsEnabled,
                KeepOriginImage = ws.KeepOriginImage,
                FontSize = ws.FontSize,
                WatermarkText = ws.WatermarkText
            };

            return View(vm);
        }

        [HttpPost("watermark-settings")]
        public async Task<IActionResult> WatermarkSettings(WatermarkSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ws = _blogConfig.WatermarkSettings;
                ws.IsEnabled = model.IsEnabled;
                ws.KeepOriginImage = model.KeepOriginImage;
                ws.FontSize = model.FontSize;
                ws.WatermarkText = model.WatermarkText;

                var response = await _blogConfig.SaveConfigurationAsync(ws);
                _blogConfig.RequireRefresh();
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
                    return BadRequest();
                }

                try
                {
                    using var bmp = new Bitmap(new MemoryStream(base64Chars));
                    if (bmp.Height != bmp.Width || bmp.Height + bmp.Width != 600)
                    {
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
            return View();
        }

        [HttpPost("shutdown")]
        public IActionResult Shutdown(int nonce, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"Shutdown is requested. Nonce value: {nonce}");
            applicationLifetime.StopApplication();
            return Ok();
        }

        [HttpPost("reset")]
        public IActionResult Reset(int nonce, [FromServices] IConfiguration configuration, [FromServices] IHostApplicationLifetime applicationLifetime)
        {
            Logger.LogWarning($"System reset is requested by {User.Identity.Name}, IP: {HttpContext.Connection.RemoteIpAddress}. Nonce value: {nonce}");
            var conn = configuration.GetConnectionString(Constants.DbConnectionName);
            var setupHelper = new SetupHelper(conn);
            var response = setupHelper.ClearData();
            if (!response.IsSuccess) return ServerError(response.Message);
            applicationLifetime.StopApplication();
            return Ok();
        }

        #endregion
    }
}