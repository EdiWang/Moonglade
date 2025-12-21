using Edi.Captcha;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

public class SignInModel(IOptions<AuthenticationSettings> authSettings,
        ICommandMediator commandMediator,
        ILogger<SignInModel> logger,
        IStatelessCaptcha captcha)
    : PageModel
{
    private readonly AuthenticationSettings _authenticationSettings = authSettings.Value;

    [BindProperty]
    [Required]
    [Display(Name = "Username")]
    [MinLength(2), MaxLength(32)]
    [RegularExpression("[a-z0-9]+")]
    public string Username { get; set; }

    [BindProperty]
    [Required]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    [MinLength(8), MaxLength(32)]
    [RegularExpression("^(?=.*[a-zA-Z])(?=.*[0-9])[A-Za-z0-9._~!@#$^&*]{8,}$")]
    public string Password { get; set; }

    [BindProperty]
    [Required]
    [StringLength(4)]
    public string CaptchaCode { get; set; }

    [BindProperty]
    [Required]
    public string CaptchaToken { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        switch (_authenticationSettings.Provider)
        {
            case AuthenticationProvider.EntraID:
                return Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    OpenIdConnectDefaults.AuthenticationScheme);
            case AuthenticationProvider.Local:
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                break;
            default:
                Response.StatusCode = StatusCodes.Status501NotImplemented;
                return Content("Invalid AuthenticationProvider, please check system settings.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!captcha.Validate(CaptchaCode, CaptchaToken))
            {
                ModelState.AddModelError(nameof(CaptchaCode), "Wrong Captcha Code");
            }

            // Check User-Agent
            var ua = Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(ua))
            {
                return Unauthorized();
            }

            if (ModelState.IsValid)
            {
                var isValid = await commandMediator.SendAsync(new ValidateLoginCommand(Username, Password));
                if (isValid)
                {
                    var claims = new List<Claim>
                    {
                        new (ClaimTypes.Name, Username),
                        new (ClaimTypes.Role, "Administrator")
                    };
                    var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var p = new ClaimsPrincipal(ci);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, p);
                    await commandMediator.SendAsync(new LogSuccessLoginCommand(ClientIPHelper.GetClientIP(HttpContext), ua));

                    var successMessage = $@"Authentication success for local account ""{Username}""";

                    logger.LogInformation(successMessage);

                    return RedirectToPage("/Admin/Post");
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                return Page();
            }

            var failMessage = $@"Authentication failed for local account ""{Username}""";

            logger.LogWarning(failMessage);

            Response.StatusCode = StatusCodes.Status400BadRequest;
            ModelState.AddModelError(string.Empty, "Bad Request.");
            return Page();
        }
        catch (Exception e)
        {
            logger.LogWarning($@"Authentication failed for local account ""{Username}""");

            ModelState.AddModelError(string.Empty, e.Message);
            return Page();
        }
    }
}