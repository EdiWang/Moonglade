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
using Moonglade.Configuration.Settings;
using Moonglade.Pages;
using Moonglade.Web.Models;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IPageService _pageService;
        private readonly ICommentService _commentService;

        private readonly IBlogAudit _blogAudit;

        public AdminController(
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            IPageService pageService,
            ICommentService commentService)
        {
            _authenticationSettings = authSettings.Value;
            _pageService = pageService;
            _commentService = commentService;

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

            return Redirect("/admin/post");
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

        [HttpGet("page/create")]
        public IActionResult CreatePage()
        {
            var model = new PageEditModel();
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