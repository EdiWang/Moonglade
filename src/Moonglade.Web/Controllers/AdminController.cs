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
using Moonglade.Comments;
using Moonglade.Configuration.Abstraction;
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
        private readonly ILocalAccountService _localAccountService;
        private readonly ICategoryService _categoryService;
        private readonly IFriendLinkService _friendLinkService;
        private readonly IPageService _pageService;
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _blogAudit;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger,
            IOptions<AuthenticationSettings> authSettings,
            IBlogAudit blogAudit,
            ILocalAccountService localAccountService,
            ICategoryService categoryService,
            IFriendLinkService friendLinkService,
            IPageService pageService,
            IBlogConfig blogConfig)
        {
            _logger = logger;
            _authenticationSettings = authSettings.Value;
            _blogAudit = blogAudit;
            _localAccountService = localAccountService;
            _categoryService = categoryService;
            _friendLinkService = friendLinkService;
            _pageService = pageService;
            _blogConfig = blogConfig;
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

                        return RedirectToAction("Index");
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
            return Forbid();
        }

        #endregion

        [HttpGet("about")]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet("category")]
        public async Task<IActionResult> Category()
        {
            var cats = await _categoryService.GetAllAsync();
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

        [Route("tags")]
        public async Task<IActionResult> Tags([FromServices] ITagService tagService)
        {
            var tags = await tagService.GetAllAsync();
            return View(tags);
        }

        [Route("comments")]
        public async Task<IActionResult> Comments([FromServices] ICommentService commentService, int page = 1)
        {
            const int pageSize = 10;
            var comments = await commentService.GetCommentsAsync(pageSize, page);
            var list =
                new StaticPagedList<CommentDetailedItem>(comments, page, pageSize, commentService.Count());
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
        public async Task<IActionResult> Pingback([FromServices] IPingbackService pingbackService)
        {
            var list = await pingbackService.GetPingbackHistoryAsync();
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