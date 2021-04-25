using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Configuration.Settings;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly IBlogAudit _blogAudit;

        public AdminController(
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit)
        {
            _authenticationSettings = authSettings.Value;
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