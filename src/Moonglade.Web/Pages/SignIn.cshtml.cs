using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Edi.Captcha;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Auth;

namespace Moonglade.Web.Pages
{
    public class SignInModel : PageModel
    {
        private readonly AuthenticationSettings _authenticationSettings;
        private readonly ILocalAccountService _localAccountService;
        private readonly ILogger<SignInModel> _logger;
        private readonly IBlogAudit _blogAudit;
        private readonly ISessionBasedCaptcha _captcha;

        public SignInModel(
            IOptions<AuthenticationSettings> authSettings,
            ILocalAccountService localAccountService,
            ILogger<SignInModel> logger,
            IBlogAudit blogAudit, ISessionBasedCaptcha captcha)
        {
            _localAccountService = localAccountService;
            _logger = logger;
            _blogAudit = blogAudit;
            _captcha = captcha;
            _authenticationSettings = authSettings.Value;
        }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a username.")]
        [Display(Name = "Username")]
        [MinLength(2, ErrorMessage = "Username must be at least 2 characters"), MaxLength(32)]
        [RegularExpression("[a-z0-9]+", ErrorMessage = "Username must be lower case letters or numbers.")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Please enter a password.")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters"), MaxLength(32)]
        [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$", ErrorMessage = "Password must be minimum eight characters, at least one letter and one number")]
        public string Password { get; set; }

        [BindProperty]
        [Required]
        [StringLength(4)]
        public string CaptchaCode { get; set; }

        public async Task<IActionResult> OnGetAsync()
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (!_captcha.Validate(CaptchaCode, HttpContext.Session))
                {
                    ModelState.AddModelError(nameof(CaptchaCode), "Wrong Captcha Code");
                }

                if (ModelState.IsValid)
                {
                    var uid = await _localAccountService.ValidateAsync(Username, Password);
                    if (uid != Guid.Empty)
                    {
                        var claims = new List<Claim>
                        {
                            new (ClaimTypes.Name, Username),
                            new (ClaimTypes.Role, "Administrator"),
                            new ("uid", uid.ToString())
                        };
                        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var p = new ClaimsPrincipal(ci);

                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);
                        await _localAccountService.LogSuccessLoginAsync(uid,
                            HttpContext.Connection.RemoteIpAddress?.ToString());

                        var successMessage = $@"Authentication success for local account ""{Username}""";

                        _logger.LogInformation(successMessage);
                        await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginSuccessLocal, successMessage);

                        return RedirectToAction("Index", "Admin");
                    }
                    ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                    return Page();
                }

                var failMessage = $@"Authentication failed for local account ""{Username}""";

                _logger.LogWarning(failMessage);
                await _blogAudit.AddAuditEntry(EventType.Authentication, AuditEventId.LoginFailedLocal, failMessage);

                Response.StatusCode = StatusCodes.Status400BadRequest;
                ModelState.AddModelError(string.Empty, "Bad Request.");
                return Page();
            }
            catch (Exception e)
            {
                _logger.LogWarning($@"Authentication failed for local account ""{Username}""");

                ModelState.AddModelError(string.Empty, e.Message);
                return Page();
            }
        }
    }
}
