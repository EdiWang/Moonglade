using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Comments;
using Moonglade.Configuration.Abstraction;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.FriendLink;
using Moonglade.Menus;
using Moonglade.Pages;
using Moonglade.Pingback;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly ICategoryService _categoryService;
        private readonly IFriendLinkService _friendLinkService;
        private readonly IPageService _pageService;
        private readonly ICommentService _commentService;
        private readonly IPingbackService _pingbackService;
        private readonly ILocalAccountService _accountService;

        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;

        public AdminController(
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            ICategoryService categoryService,
            IFriendLinkService friendLinkService,
            IPageService pageService,
            ICommentService commentService,
            IPingbackService pingbackService,
            ILocalAccountService accountService,
            IBlogConfig blogConfig)
        {
            _authenticationSettings = authSettings.Value;
            _categoryService = categoryService;
            _friendLinkService = friendLinkService;
            _pageService = pageService;
            _commentService = commentService;
            _pingbackService = pingbackService;
            _accountService = accountService;

            _blogConfig = blogConfig;
            _blogAudit = blogAudit;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            if (_authenticationSettings.Provider == AuthenticationProvider.AzureAD)
            {
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessAAD,
                    $"Authentication success for Azure account '{User.Identity?.Name}'");
            }

            return RedirectToAction("Post");
        }

        [HttpGet("auditlogs")]
        public async Task<IActionResult> AuditLogs([FromServices] IFeatureManager featureManager, int page = 1)
        {
            var flag = await featureManager.IsEnabledAsync(nameof(FeatureFlags.EnableAudit));
            if (!flag) return View();

            if (page <= 0) return BadRequest(ModelState);

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

        [HttpGet("category")]
        public async Task<IActionResult> Category()
        {
            var cats = await _categoryService.GetAll();
            return View(new CategoryManageModel { Categories = cats });
        }

        [HttpGet("post")]
        public IActionResult Post()
        {
            return View();
        }

        [HttpGet("page")]
        public async Task<IActionResult> Page()
        {
            var pageSegments = await _pageService.ListSegment();
            return View(pageSegments);
        }

        [HttpGet("page/create")]
        public IActionResult CreatePage()
        {
            var model = new PageEditModel();
            return View("EditPage", model);
        }

        [HttpGet("page/edit/{id:guid}")]
        public async Task<IActionResult> EditPage(Guid id)
        {
            var page = await _pageService.GetAsync(id);
            if (page is null) return NotFound();

            var model = new PageEditModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                MetaDescription = page.MetaDescription,
                CssContent = page.CssContent,
                RawHtmlContent = page.RawHtmlContent,
                HideSidebar = page.HideSidebar,
                IsPublished = page.IsPublished
            };

            return View("EditPage", model);
        }

        [Route("/page/preview/{pageId:guid}")]
        public async Task<IActionResult> PreviewPage(Guid pageId)
        {
            var page = await _pageService.GetAsync(pageId);
            if (page is null) return NotFound();

            ViewBag.IsDraftPreview = true;

            return View("~/Views/Home/Page.cshtml", page);
        }

        [Route("comments")]
        public async Task<IActionResult> Comments(int page = 1)
        {
            const int pageSize = 10;
            var comments = await _commentService.GetCommentsAsync(pageSize, page);
            var list = new StaticPagedList<CommentDetailedItem>(comments, page, pageSize, _commentService.Count());
            return View(list);
        }

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

        [Route("pingback")]
        public async Task<IActionResult> Pingback()
        {
            var list = await _pingbackService.GetPingbackHistoryAsync();
            return View(list);
        }

        [HttpGet("account")]
        public async Task<IActionResult> LocalAccount()
        {
            var accounts = await _accountService.GetAllAsync();
            var vm = new AccountManageViewModel { Accounts = accounts };

            return View(vm);
        }

        // Keep session from expire when writing a very long post
        [IgnoreAntiforgeryToken]
        [Route("keep-alive")]
        public IActionResult KeepAlive(string nonce)
        {
            return Json(new
            {
                ServerTime = DateTime.UtcNow,
                Nonce = nonce
            });
        }
    }
}