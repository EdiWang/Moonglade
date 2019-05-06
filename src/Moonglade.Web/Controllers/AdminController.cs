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
using Moonglade.Model;
using Moonglade.Web.Models;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : MoongladeController
    {
        private readonly bool isAADAuth;

        public AdminController(ILogger<AdminController> logger)
            : base(logger)
        {
            var authenticationProvider = AppDomain.CurrentDomain.GetData("AuthenticationProvider").ToString();
            switch (authenticationProvider)
            {
                case "AzureAd":
                    isAADAuth = true;
                    break;
                case "Local":
                    isAADAuth = false;
                    break;
            }
        }

        [Route("")]
        public IActionResult Index()
        {
            return RedirectToAction("Manage", "Post");
        }

        #region Authentication

        [HttpGet("signin")]
        [AllowAnonymous]
        public async Task<IActionResult> SignIn()
        {
            if (isAADAuth)
            {
                var redirectUrl = Url.Action(nameof(PostController.Index), "Post");
                return Challenge(
                    new AuthenticationProperties { RedirectUri = redirectUrl },
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            await HttpContext.SignOutAsync(Constants.CookieAuthSchemeName);
            return View();
        }

        [HttpPost]
        [Route("signin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // TODO: Perform Auth logic

                    var claims = new List<Claim>
                    {
                        new Claim("name", model.Username),
                        new Claim("role", "Administrator")
                    };
                    var ci = new ClaimsIdentity(claims, "Password", "name", "role");
                    var p = new ClaimsPrincipal(ci);
                    await HttpContext.SignInAsync(Constants.CookieAuthSchemeName, p);
                    Logger.LogInformation($@"Authentication success for local account ""{model.Username}""");

                    return RedirectToAction("Index");
                }

                Logger.LogWarning($@"Authentication failed for local account ""{model.Username}""");

                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
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
            if (isAADAuth)
            {
                var callbackUrl = Url.Action(nameof(SignedOut), "Admin", values: null, protocol: Request.Scheme);
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme);
            }

            await HttpContext.SignOutAsync(Constants.CookieAuthSchemeName);
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