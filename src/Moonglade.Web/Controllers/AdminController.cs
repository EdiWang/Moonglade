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
using Moonglade.Core;
using Moonglade.Model;
using Moonglade.Pingback;
using Moonglade.Web.Authentication;
using Moonglade.Web.Models;
using Moonglade.Web.Models.Settings;
using X.PagedList;

namespace Moonglade.Web.Controllers
{
    [Authorize]
    [Route("admin")]
    public class AdminController : BlogController
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly LocalAccountService _localAccountService;
        private readonly IBlogAudit _blogAudit;

        public AdminController(
            ILogger<AdminController> logger,
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            LocalAccountService localAccountService)
            : base(logger)
        {
            _blogAudit = blogAudit;
            _localAccountService = localAccountService;
            _authenticationSettings = authSettings.Value;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            if (_authenticationSettings.Provider == AuthenticationProvider.AzureAD)
            {
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessAAD,
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
                    var redirectUrl = Url.Action(nameof(HomeController.Index), "Home");
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
                    var uid = await _localAccountService.ValidateAsync(model.Username, model.Password);
                    if (uid != Guid.Empty)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.Username),
                            new Claim(ClaimTypes.Role, "Administrator"),
                            new Claim("uid", uid.ToString())
                        };
                        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var p = new ClaimsPrincipal(ci);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);
                        await _localAccountService.LogSuccessLoginAsync(uid,
                            HttpContext.Connection.RemoteIpAddress.ToString());

                        var successMessage = $@"Authentication success for local account ""{model.Username}""";

                        Logger.LogInformation(successMessage);
                        await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessLocal, successMessage);

                        return RedirectToAction("Index");
                    }
                    ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                    return View(model);
                }

                var failMessage = $@"Authentication failed for local account ""{model.Username}""";

                Logger.LogWarning(failMessage);
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginFailedLocal, failMessage);

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

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("signedout")]
        [AllowAnonymous]
        public IActionResult SignedOut()
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [AllowAnonymous]
        [HttpGet("accessdenied")]
        public IActionResult AccessDenied()
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("category")]
        public async Task<IActionResult> Category([FromServices] CategoryService categoryService)
        {
            var cats = await categoryService.GetAllAsync();
            return View(new CategoryManageViewModel { Categories = cats });
        }

        [HttpGet("page")]
        public async Task<IActionResult> Page([FromServices] PageService pageService)
        {
            var pageSegments = await pageService.ListSegmentAsync();
            return View(pageSegments);
        }

        [Route("tags")]
        public async Task<IActionResult> Tags([FromServices] TagService tagService)
        {
            var tags = await tagService.GetAllAsync();
            return View(tags);
        }

        [Route("comments")]
        public async Task<IActionResult> Comments([FromServices] CommentService commentService, int page = 1)
        {
            const int pageSize = 10;
            var comments = await commentService.GetCommentsAsync(pageSize, page);
            var list =
                new StaticPagedList<CommentDetailedItem>(comments, page, pageSize, commentService.Count());
            return View(list);
        }

        [HttpGet("menu")]
        public async Task<IActionResult> Menu([FromServices] MenuService menuService)
        {
            var menus = await menuService.GetAllAsync();
            var model = new MenuManageViewModel
            {
                MenuItems = menus
            };

            return View(model);
        }

        [Route("pingback")]
        public async Task<IActionResult> Pingback([FromServices] IPingbackService pingbackService)
        {
            var list = await pingbackService.GetPingbackHistoryAsync();
            return View(list);
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