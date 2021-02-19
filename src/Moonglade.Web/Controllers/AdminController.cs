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
        private readonly ITagService _tagService;
        private readonly ICommentService _commentService;
        private readonly IPingbackService _pingbackService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;

        public AdminController(
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            ICategoryService categoryService,
            IFriendLinkService friendLinkService,
            IPageService pageService,
            ITagService tagService,
            ICommentService commentService,
            IPingbackService pingbackService,
            IBlogConfig blogConfig)
        {
            _authenticationSettings = authSettings.Value;
            _categoryService = categoryService;
            _friendLinkService = friendLinkService;
            _pageService = pageService;
            _tagService = tagService;
            _commentService = commentService;
            _pingbackService = pingbackService;

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

        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }

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

        [HttpGet("tags")]
        public async Task<IActionResult> Tags()
        {
            var tags = await _tagService.GetAll();
            return View(tags);
        }

        [Route("comments")]
        public async Task<IActionResult> Comments(int page = 1)
        {
            const int pageSize = 10;
            var comments = await _commentService.GetCommentsAsync(pageSize, page);
            var list = new StaticPagedList<CommentDetailedItem>(comments, page, pageSize, _commentService.Count());
            return View(list);
        }

        [HttpGet("menu")]
        public async Task<IActionResult> Menu([FromServices] IMenuService menuService)
        {
            var menus = await menuService.GetAllAsync();
            var model = new MenuManageModel
            {
                MenuItems = menus
            };

            return View(model);
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