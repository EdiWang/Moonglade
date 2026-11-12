using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Pages;

[Authorize(AuthenticationSchemes = BlogAuthSchemas.LocalAccountTwoFactor)]
[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
public class VerifyAuthenticatorModel(
    IOptions<AuthenticationSettings> authSettings,
    OwnerAccountStore accounts,
    ILogger<VerifyAuthenticatorModel> logger) : PageModel
{
    [BindProperty, Required, StringLength(6, MinimumLength = 6), RegularExpression("[0-9]{6}")]
    [Display(Name = "Authenticator code")]
    public string AuthenticatorCode { get; set; }

    public async Task<IActionResult> OnGetAsync() =>
        await CurrentAccountAsync() is null ? RedirectToPage("/SignIn") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await CurrentAccountAsync();
        if (account is null) return RedirectToPage("/SignIn");
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }
        OwnerAccountSnapshot verified;
        try
        {
            verified = await accounts.VerifyTotpAsync(account.Id, account.Revision, AuthenticatorCode, HttpContext.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
            return RedirectToPage("/SignIn");
        }
        if (verified is null)
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), "Invalid authenticator code.");
            return Page();
        }
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            LocalAccountPrincipal.Create(verified, CookieAuthenticationDefaults.AuthenticationScheme));
        logger.LogInformation("Local account authentication completed");
        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task<OwnerAccountSnapshot> CurrentAccountAsync()
    {
        if (!authSettings.Value.Totp.Required) return null;
        var account = await accounts.ReadAsync(HttpContext.RequestAborted);
        return LocalAccountPrincipal.Matches(User, account) && account.IsTotpEnabled ? account : null;
    }
}
