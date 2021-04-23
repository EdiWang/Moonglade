using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly ILocalAccountService _localAccountService;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IOptions<AuthenticationSettings> authSettings,
            ILocalAccountService localAccountService,
            IBlogAudit blogAudit,
            ILogger<AuthController> logger)
        {
            _authenticationSettings = authSettings.Value;
            _localAccountService = localAccountService;
            _blogAudit = blogAudit;
            _logger = logger;
        }

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn()
        {
            switch (_authenticationSettings.Provider)
            {
                case AuthenticationProvider.AzureAD:
                    return Challenge(
                        new AuthenticationProperties { RedirectUri = "/" },
                        OpenIdConnectDefaults.AuthenticationScheme);
                case AuthenticationProvider.Local:
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    break;
                case AuthenticationProvider.None:
                    Response.StatusCode = StatusCodes.Status501NotImplemented;
                    return Content("No AuthenticationProvider is set, please check system settings.");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return View();
        }

        [HttpPost("signin")]
        [ServiceFilter(typeof(ValidateCaptcha))]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var uid = await _localAccountService.ValidateAsync(model.Username, model.Password);
                    if (uid != Guid.Empty)
                    {
                        var claims = new List<Claim>
                        {
                            new (ClaimTypes.Name, model.Username),
                            new (ClaimTypes.Role, "Administrator"),
                            new ("uid", uid.ToString())
                        };
                        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var p = new ClaimsPrincipal(ci);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);
                        await _localAccountService.LogSuccessLoginAsync(uid,
                            HttpContext.Connection.RemoteIpAddress?.ToString());

                        var successMessage = $@"Authentication success for local account ""{model.Username}""";

                        _logger.LogInformation(successMessage);
                        await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessLocal, successMessage);

                        return RedirectToAction("Index", "Admin");
                    }
                    ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                    return View(model);
                }

                var failMessage = $@"Authentication failed for local account ""{model.Username}""";

                _logger.LogWarning(failMessage);
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginFailedLocal, failMessage);

                Response.StatusCode = StatusCodes.Status400BadRequest;
                ModelState.AddModelError(string.Empty, "Bad Request.");
                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogWarning($@"Authentication failed for local account ""{model.Username}""");

                ModelState.AddModelError(string.Empty, e.Message);
                return View(model);
            }
        }

        [HttpGet("signout")]
        public async Task<IActionResult> SignOut(int nounce = 1055)
        {
            switch (_authenticationSettings.Provider)
            {
                case AuthenticationProvider.AzureAD:
                    {
                        var callbackUrl = Url.Action(nameof(SignedOut), "Admin", null, Request.Scheme);
                        return SignOut(
                            new AuthenticationProperties { RedirectUri = callbackUrl },
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            OpenIdConnectDefaults.AuthenticationScheme);
                    }
                case AuthenticationProvider.Local:
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    break;
                case AuthenticationProvider.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return RedirectToPage("Index");
        }

        [HttpGet("signedout")]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return RedirectToPage("Index");
        }

        [AllowAnonymous]
        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            return Forbid();
        }
    }
}
