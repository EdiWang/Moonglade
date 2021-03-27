using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Configuration.Settings;
using Moonglade.Pages;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IPageService _pageService;
        private readonly IBlogAudit _blogAudit;

        public AdminController(
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            IPageService pageService)
        {
            _authenticationSettings = authSettings.Value;
            _pageService = pageService;

            _blogAudit = blogAudit;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            if (_authenticationSettings.Provider == AuthenticationProvider.AzureAD)
            {
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessAAD,
                    $"Authentication success for Azure account '{User.Identity?.Name}'");
            }

            return Redirect("/admin/post");
        }

        [HttpGet("clear-auditlogs")]
        [FeatureGate(FeatureFlags.EnableAudit)]
        public async Task<IActionResult> ClearAuditLogs()
        {
            await _blogAudit.ClearAuditLog();
            return Redirect("/admin/auditlogs");
        }

        [HttpGet("/page/preview/{pageId:guid}")]
        public async Task<IActionResult> PreviewPage(Guid pageId)
        {
            var page = await _pageService.GetAsync(pageId);
            if (page is null) return NotFound();

            ViewBag.IsDraftPreview = true;

            return View("~/Views/Home/Page.cshtml", page);
        }

        // Keep session from expire when writing a very long post
        [IgnoreAntiforgeryToken]
        [HttpPost("keep-alive")]
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