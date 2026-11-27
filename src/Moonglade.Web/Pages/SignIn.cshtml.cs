using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
public class SignInModel(
    IOptions<AuthenticationSettings> authSettings,
    LocalAccountStore accounts,
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
        await HttpContext.SignOutAsync(BlogAuthSchemas.TemporaryPasswordChange);
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

        var account = await accounts.AuthenticateAsync(Email, Password, HttpContext.RequestAborted);
        if (account is null)
        {
            logger.LogWarning("Local account password sign-in failed");
            ModelState.AddModelError(string.Empty, "Invalid Login Attempt.");
            return Page();
        }
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        await HttpContext.SignOutAsync(BlogAuthSchemas.TemporaryPasswordChange);
        var scheme = account.MustChangePassword
            ? BlogAuthSchemas.TemporaryPasswordChange
            : !authSettings.Value.Totp.Required
                ? BlogAuthSchemas.Local
                : account.IsTotpEnabled ? BlogAuthSchemas.LocalAccountTwoFactor : BlogAuthSchemas.LocalAccountSetup;
        if (scheme == BlogAuthSchemas.LocalAccountTwoFactor)
        {
            var remembered = await HttpContext.AuthenticateAsync(BlogAuthSchemas.LocalAccountRememberedDevice);
            if (remembered.Succeeded && LocalAccountPrincipal.MatchesAccount(remembered.Principal, account))
                scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        }
        if (scheme != BlogAuthSchemas.Local && User.Identity?.IsAuthenticated == true)
            await HttpContext.SignOutAsync(BlogAuthSchemas.Local);
        await HttpContext.SignInAsync(scheme, LocalAccountPrincipal.Create(account, scheme));
        return scheme switch
        {
            BlogAuthSchemas.Local => RedirectToPage(account.IsOwner ? "/Admin/Dashboard" : "/Admin/Post"),
            BlogAuthSchemas.TemporaryPasswordChange => RedirectToPage("/ChangeTemporaryPassword"),
            BlogAuthSchemas.LocalAccountTwoFactor => RedirectToPage("/VerifyAuthenticator"),
            _ => RedirectToPage("/SetupAuthenticator")
        };
    }
}
