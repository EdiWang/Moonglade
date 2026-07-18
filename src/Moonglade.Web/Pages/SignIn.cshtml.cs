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
        IBlogConfig blogConfig)
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
                await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
                await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
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
            switch (_authenticationSettings.Provider)
            {
                case AuthenticationProvider.Local:
                    break;
                case AuthenticationProvider.EntraID:
                    return Challenge(
                        new AuthenticationProperties { RedirectUri = "/" },
                        OpenIdConnectDefaults.AuthenticationScheme);
                default:
                    Response.StatusCode = StatusCodes.Status501NotImplemented;
                    return Content("Invalid AuthenticationProvider, please check system settings.");
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
                    var account = blogConfig.LocalAccountSettings;

                    if (!IsTotpConfigured(account))
                    {
                        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
                        await SignInSetupAsync(account.Username);
                        return RedirectToPage("/SetupAuthenticator");
                    }

                    await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
                    await SignInTwoFactorAsync(account.Username);
                    return RedirectToPage("/VerifyAuthenticator");
                }
                ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
                return Page();
            }

            logger.LogWarning("Authentication failed for local account '{Username}'", Username);

            Response.StatusCode = StatusCodes.Status400BadRequest;
            ModelState.AddModelError(string.Empty, "Bad Request.");
            return Page();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Authentication failed for local account '{Username}'", Username);

            ModelState.AddModelError(string.Empty, "An error occurred during authentication. Please try again later.");
            return Page();
        }
    }

    private static bool IsTotpConfigured(LocalAccountSettings account) =>
        account.IsTotpEnabled && !string.IsNullOrWhiteSpace(account.TotpSecret);

    private async Task SignInSetupAsync(string username)
    {
        var principal = CreatePrincipal(username, BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignInAsync(BlogAuthSchemas.LocalAccountSetup, principal);
    }

    private async Task SignInTwoFactorAsync(string username)
    {
        var principal = CreatePrincipal(username, BlogAuthSchemas.LocalAccountTwoFactor);
        await HttpContext.SignInAsync(BlogAuthSchemas.LocalAccountTwoFactor, principal);
    }

    private static ClaimsPrincipal CreatePrincipal(string username, string authenticationType)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Administrator")
        };
        var ci = new ClaimsIdentity(claims, authenticationType);
        return new ClaimsPrincipal(ci);
    }
}
