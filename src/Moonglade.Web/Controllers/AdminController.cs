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
using Moonglade.Web.Authentication;
using Moonglade.Web.Models;
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : MoongladeController
    {
        private readonly AuthenticationSettings _authenticationSettings;

        private readonly IMoongladeAudit _moongladeAudit;

        public AdminController(
            ILogger<AdminController> logger,
            IOptions<AuthenticationSettings> authSettings,
            IMoongladeAudit moongladeAudit)
            : base(logger)
        {
            _moongladeAudit = moongladeAudit;
            _authenticationSettings = authSettings.Value;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            if (_authenticationSettings.Provider == AuthenticationProvider.AzureAD)
            {
                await _moongladeAudit.AddAuditEntry(EventType.Authentication, EventId.LoginSuccessAAD,
                    $"Authentication success for Azure account '{User.Identity.Name}'");
            }

            return RedirectToAction("Manage", "Post");
        }

        #region Authentication

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn()
        {
            switch (_authenticationSettings.Provider)
            {
                case AuthenticationProvider.AzureAD:
                    var redirectUrl = Url.Action(nameof(PostController.Index), "Post");
                    return Challenge(
                        new AuthenticationProperties { RedirectUri = redirectUrl },
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
        [AllowAnonymous]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.Username == _authenticationSettings.Local.Username &&
                        model.Password == _authenticationSettings.Local.Password)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim(ClaimTypes.Role, "Administrator")
                        };
                        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var p = new ClaimsPrincipal(ci);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);

                        var successMessage = $@"Authentication success for local account ""{model.Username}""";

                        Logger.LogInformation(successMessage);
                        await _moongladeAudit.AddAuditEntry(EventType.Authentication, EventId.LoginSuccessLocal, successMessage);

                        return RedirectToAction("Index");
                    }
                    ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                    return View(model);
                }

                var failMessage = $@"Authentication failed for local account ""{model.Username}""";

                Logger.LogWarning(failMessage);
                await _moongladeAudit.AddAuditEntry(EventType.Authentication, EventId.LoginFailedLocal, failMessage);

                Response.StatusCode = StatusCodes.Status400BadRequest;
                ModelState.AddModelError(string.Empty, "Bad Request.");
                return View(model);
            }
            catch (Exception e)
            {
                Logger.LogWarning($@"Authentication failed for local account ""{model.Username}""");

                ModelState.AddModelError(string.Empty, e.Message);
                return View(model);
            }
        }

        [HttpGet("signout")]
        public async Task<IActionResult> SignOut()
        {
            Logger.LogInformation($"User '{User.Identity.Name}' signing out.'");

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

            return RedirectToAction("Index", "Post");
        }

        [HttpGet("signedout")]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return RedirectToAction(nameof(PostController.Index), "Post");
        }

        [AllowAnonymous]
        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        #endregion

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