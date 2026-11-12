using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

[Authorize(AuthenticationSchemes = BlogAuthSchemas.LocalAccountSetup)]
[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
public class SetupAuthenticatorModel(
    IOptions<AuthenticationSettings> authSettings,
    OwnerAccountStore accounts,
    ILocalAccountTotpService totpService,
    ILogger<SetupAuthenticatorModel> logger) : PageModel
{
    private const string SecretClaim = "moonglade:totp-secret";
    public string SetupKey { get; private set; }
    public string QrCodeImageUri { get; private set; }

    [BindProperty, Required, StringLength(6, MinimumLength = 6), RegularExpression("[0-9]{6}")]
    [Display(Name = "Authenticator code")]
    public string AuthenticatorCode { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await CurrentAccountAsync();
        if (account is null) return RedirectToPage("/SignIn");
        var ticket = await HttpContext.AuthenticateAsync(BlogAuthSchemas.LocalAccountSetup);
        var secret = ticket.Principal?.FindFirstValue(SecretClaim);
        if (string.IsNullOrWhiteSpace(secret))
        {
            secret = totpService.GenerateSecret();
            var principal = LocalAccountPrincipal.Create(account, BlogAuthSchemas.LocalAccountSetup);
            ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim(SecretClaim, secret));
            await HttpContext.SignInAsync(BlogAuthSchemas.LocalAccountSetup, principal, ticket.Properties);
        }
        Display(account.Email, secret);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await CurrentAccountAsync();
        var secret = User.FindFirstValue(SecretClaim);
        if (account is null || string.IsNullOrWhiteSpace(secret)) return RedirectToPage("/SignIn");
        if (!ModelState.IsValid)
        {
            Display(account.Email, secret);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }
        OwnerAccountSnapshot saved;
        try
        {
            saved = await accounts.EnrollTotpAsync(account.Id, account.Revision, secret, AuthenticatorCode, HttpContext.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
            return RedirectToPage("/SignIn");
        }
        if (saved is null)
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), "Invalid authenticator code.");
            Display(account.Email, secret);
            return Page();
        }
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            LocalAccountPrincipal.Create(saved, CookieAuthenticationDefaults.AuthenticationScheme));
        logger.LogInformation("Local account authenticator enrollment completed");
        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task<OwnerAccountSnapshot> CurrentAccountAsync()
    {
        if (!authSettings.Value.Totp.Required) return null;
        var account = await accounts.ReadAsync(HttpContext.RequestAborted);
        return LocalAccountPrincipal.Matches(User, account) && !account.IsTotpEnabled ? account : null;
    }

    private void Display(string email, string secret)
    {
        SetupKey = secret;
        QrCodeImageUri = AuthenticatorQrCode.Create(totpService.BuildAuthenticatorUri(authSettings.Value.Totp.Issuer, email, secret));
    }
}
