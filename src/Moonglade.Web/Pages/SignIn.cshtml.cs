using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
public class SignInModel(
    IOptions<AuthenticationSettings> authSettings,
    OwnerAccountStore accounts,
    ILogger<SignInModel> logger) : PageModel
{
    [BindProperty, Required, EmailAddress, StringLength(254)]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [BindProperty, Required, DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    public bool OidcFailed { get; private set; }

    public async Task<IActionResult> OnGetAsync(bool failed = false)
    {
        OidcFailed = failed;
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        return Page();
    }

    public async Task<IActionResult> OnPostOidcAsync([FromServices] OwnerOidcStore oidc, [FromServices] OidcBindingFlow flow)
    {
        var option = await oidc.ReadSignInOptionAsync(HttpContext.RequestAborted);
        return option is null
            ? RedirectToPage(new RouteValueDictionary { ["failed"] = true })
            : flow.StartSignIn(option.Revision, Url.Page("/Admin/Dashboard"));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Request.Headers.UserAgent)) return Unauthorized();
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        var account = await accounts.AuthenticateLocalAsync(Email, Password, HttpContext.RequestAborted);
        if (account is null)
        {
            logger.LogWarning("Local account password sign-in failed");
            ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
            return Page();
        }
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        var scheme = !authSettings.Value.Totp.Required
            ? CookieAuthenticationDefaults.AuthenticationScheme
            : account.IsTotpEnabled ? BlogAuthSchemas.LocalAccountTwoFactor : BlogAuthSchemas.LocalAccountSetup;
        await HttpContext.SignInAsync(scheme, LocalAccountPrincipal.Create(account, scheme));
        if (scheme == CookieAuthenticationDefaults.AuthenticationScheme)
            return RedirectToPage("/Admin/Dashboard");
        return RedirectToPage(account.IsTotpEnabled ? "/VerifyAuthenticator" : "/SetupAuthenticator");
    }
}
