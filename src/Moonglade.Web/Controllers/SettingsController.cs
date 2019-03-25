using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Edi.Net.AesEncryption;
using Edi.Practice.RequestResponseModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin/settings")]
    public class SettingsController : MoongladeController
    {
        #region Private Fields

        private readonly EmailService _emailService;
        private readonly FriendLinkService _friendLinkService;
        private readonly BlogConfig _blogConfig;
        private readonly BlogConfigurationService _blogConfigurationService;
        private readonly AesEncryptionService _encryptionService;

        #endregion

        public SettingsController(MoongladeDbContext context,
            ILogger<SettingsController> logger,
            IOptionsSnapshot<AppSettings> settings,
            IConfiguration configuration,
            IHttpContextAccessor accessor,
            IMemoryCache memoryCache,
            EmailService emailService,
            FriendLinkService friendLinkService,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            AesEncryptionService encryptionService)
            : base(context, logger, settings, configuration, accessor, memoryCache)
        {
            _blogConfig = blogConfig;
            _blogConfigurationService = blogConfigurationService;
            _encryptionService = encryptionService;
            _blogConfig.GetConfiguration(blogConfigurationService);

            _emailService = emailService;
            _friendLinkService = friendLinkService;
        }

        [Route("general")]
        public IActionResult General()
        {
            var vm = new GeneralSettingsViewModel
            {
                DisharmonyWords = _blogConfig.DisharmonyWords,
                MetaAuthor = _blogConfig.MetaAuthor,
                MetaKeyword = _blogConfig.MetaKeyword,
                SiteTitle = _blogConfig.SiteTitle,
                ReaderViewSiteName = _blogConfig.ReaderView.SiteName,
                EnableComments = _blogConfig.EnableComments
            };
            return View(vm);
        }

        [HttpPost]
        [Route("general")]
        public IActionResult General(GeneralSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                _blogConfig.DisharmonyWords = model.DisharmonyWords;
                _blogConfig.MetaAuthor = model.MetaAuthor;
                _blogConfig.MetaKeyword = model.MetaKeyword;
                _blogConfig.SiteTitle = model.SiteTitle;
                _blogConfig.ReaderView.SiteName = model.ReaderViewSiteName;
                _blogConfig.EnableComments = model.EnableComments;

                var response = _blogConfigurationService.SaveGeneralSettings(_blogConfig);
                _blogConfig.DumpOldValuesWhenNextLoad();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #region Email Settings

        [Route("email-settings")]
        public IActionResult EmailSettings()
        {
            var ec = _blogConfig.EmailConfiguration;
            var vm = new EmailSettingsViewModel
            {
                AdminEmail = ec.AdminEmail,
                BannedMailDomain = ec.BannedMailDomain,
                EmailDisplayName = ec.EmailDisplayName,
                EnableEmailSending = ec.EnableEmailSending,
                EnableSsl = ec.EnableSsl,
                SendEmailOnCommentReply = ec.SendEmailOnCommentReply,
                SendEmailOnNewComment = ec.SendEmailOnNewComment,
                SmtpServer = ec.SmtpServer,
                SmtpServerPort = ec.SmtpServerPort,
                SmtpUserName = ec.SmtpUserName,
                // SmtpPassword = ec.SmtpPassword
            };
            return View(vm);
        }

        [HttpPost]
        [Route("email-settings")]
        public IActionResult EmailSettings(EmailSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ec = _blogConfig.EmailConfiguration;
                ec.AdminEmail = model.AdminEmail;
                ec.BannedMailDomain = model.BannedMailDomain;
                ec.EmailDisplayName = model.EmailDisplayName;
                ec.EnableEmailSending = model.EnableEmailSending;
                ec.EnableSsl = model.EnableSsl;
                ec.SendEmailOnCommentReply = model.SendEmailOnCommentReply;
                ec.SendEmailOnNewComment = model.SendEmailOnNewComment;
                ec.SmtpServer = model.SmtpServer;
                ec.SmtpServerPort = model.SmtpServerPort;
                ec.SmtpUserName = model.SmtpUserName;
                if (!string.IsNullOrWhiteSpace(model.SmtpPassword))
                {
                    ec.SmtpPassword = _blogConfigurationService.EncryptPassword(model.SmtpPassword);
                }

                var response = _blogConfigurationService.SaveEmailConfiguration(ec);
                _blogConfig.DumpOldValuesWhenNextLoad();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        [HttpPost]
        [Route("send-test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            var response = await _emailService.TestSendTestMailAsync();
            return Json(response);
        }

        [HttpGet("generate-new-aes-keys")]
        public IActionResult GenerateNewAesKeys()
        {
            var aesAlg = Aes.Create();
            var key = aesAlg.Key;
            var iv = aesAlg.IV;
            var resp = new
            {
                Key = key,
                Iv = iv,
                GenTimeUtc = DateTime.UtcNow
            };
            return Json(resp);
        }

        #endregion

        #region Feed Settings

        [Route("feed-settings")]
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

        [HttpPost]
        [Route("feed-settings")]
        public IActionResult FeedSettings(FeedSettingsViewModel model)
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

                var response = _blogConfigurationService.SaveFeedConfiguration(fs);
                _blogConfig.DumpOldValuesWhenNextLoad();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #endregion

        #region Watermark Settings

        [Route("watermark-settings")]
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

        [HttpPost]
        [Route("watermark-settings")]
        public IActionResult WatermarkSettings(WatermarkSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ws = _blogConfig.WatermarkSettings;
                ws.IsEnabled = model.IsEnabled;
                ws.KeepOriginImage = model.KeepOriginImage;
                ws.FontSize = model.FontSize;
                ws.WatermarkText = model.WatermarkText;

                var response = _blogConfigurationService.SaveWatermarkConfiguration(ws);
                _blogConfig.DumpOldValuesWhenNextLoad();
                return Json(response);
            }
            return Json(new FailedResponse((int)ResponseFailureCode.InvalidModelState, "Invalid ModelState"));
        }

        #endregion

        #region FriendLinks

        [HttpGet("manage-friendlinks")]
        public async Task<IActionResult> ManageFriendLinks()
        {
            var response = await _friendLinkService.GetAllFriendLinksAsync();
            if (response.IsSuccess)
            {
                return View(response.Item);
            }

            ViewBag.HasError = true;
            ViewBag.ErrorMessage = response.Message;
            return View(new List<FriendLink>());
        }

        [HttpGet("create-friendlink")]
        public IActionResult CreateFriendLink()
        {
            return View("CreateOrEditFriendLink", new FriendLinkEditViewModel());
        }

        [HttpPost("create-friendlink")]
        public IActionResult CreateFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var response = _friendLinkService.AddFriendLink(viewModel.Title, viewModel.LinkUrl);
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
        public IActionResult EditFriendLink(Guid id)
        {
            try
            {
                var response = _friendLinkService.GetFriendLink(id);
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
        public IActionResult EditFriendLink(FriendLinkEditViewModel viewModel)
        {
            try
            {
                var response = _friendLinkService.UpdateFriendLink(viewModel.Id, viewModel.Title, viewModel.LinkUrl);
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
        public IActionResult DeleteFriendLink(Guid id)
        {
            var response = _friendLinkService.DeleteFriendLink(id);
            if (response.IsSuccess)
            {
                return RedirectToAction(nameof(ManageFriendLinks));
            }

            return ServerError();
        }

        #endregion

        #region User Avatar

        [HttpPost]
        [Route("set-blogger-avatar")]
        public async Task<IActionResult> SetBloggerAvatar(IFormFile avatarimage)
        {
            try
            {
                if (null == avatarimage)
                {
                    Logger.LogError("file is null.");
                    return BadRequest();
                }

                if (avatarimage.Length > 0)
                {
                    var name = Path.GetFileName(avatarimage.FileName);
                    if (name == null) return BadRequest();

                    var ext = Path.GetExtension(name).ToLower();
                    var allowedImageFormats = new[] { ".png", ".jpg" };
                    if (!allowedImageFormats.Contains(ext))
                    {
                        return BadRequest();
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        await avatarimage.CopyToAsync(memoryStream);
                        var imageBytes = memoryStream.ToArray();
                        var imageBase64String = Convert.ToBase64String(imageBytes);
                        _blogConfig.BloggerAvatarBase64 = imageBase64String;
                        var response = _blogConfigurationService.SaveBloggerAvatar(imageBase64String);
                        _blogConfig.DumpOldValuesWhenNextLoad();
                        return Json(response);
                    }
                }
                return BadRequest();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error uploading avatar image.");
                return ServerError();
            }
        }

        #endregion
    }
}