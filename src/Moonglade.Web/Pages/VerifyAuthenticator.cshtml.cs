using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

[Authorize(AuthenticationSchemes = BlogAuthSchemas.LocalAccountTwoFactor)]
public class VerifyAuthenticatorModel(
    IOptions<AuthenticationSettings> authSettings,
    IBlogConfig blogConfig,
    ILocalAccountTotpService totpService,
    ILogger<VerifyAuthenticatorModel> logger) : PageModel
{
    private readonly AuthenticationSettings _authenticationSettings = authSettings.Value;

    [BindProperty]
    [Required]
    [Display(Name = "Authenticator code")]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("[0-9]{6}")]
    public string AuthenticatorCode { get; set; }

    public IActionResult OnGet()
    {
        if (_authenticationSettings.Provider != AuthenticationProvider.Local)
        {
            return RedirectToPage("/Index");
        }

        if (!IsTotpConfigured(blogConfig.LocalAccountSettings))
        {
            return RedirectToPage("/SignIn");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_authenticationSettings.Provider != AuthenticationProvider.Local)
        {
            return RedirectToPage("/Index");
        }

        var account = blogConfig.LocalAccountSettings;
        if (!IsTotpConfigured(account))
        {
            return RedirectToPage("/SignIn");
        }

        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        if (!totpService.VerifyCode(account.TotpSecret, AuthenticatorCode))
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), "Invalid authenticator code.");
            return Page();
        }

        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        await SignInAdminAsync(account.Username);

        logger.LogInformation("Authentication success for local account '{Username}'", account.Username);

        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task SignInAdminAsync(string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Administrator")
        };
        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(ci);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private static bool IsTotpConfigured(LocalAccountSettings account) =>
        account.IsTotpEnabled && !string.IsNullOrWhiteSpace(account.TotpSecret);
}
