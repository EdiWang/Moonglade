using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Moonglade.Web.Controllers;

[Route("auth")]
public class AuthController(
    IOptions<AuthenticationSettings> authSettings
    ) : ControllerBase
{
    private readonly AuthenticationSettings _authenticationSettings = authSettings.Value;

    [HttpGet("signout")]
    public async Task<IActionResult> SignOut(int nounce = 996)
    {
        switch (_authenticationSettings.Provider)
        {
            case AuthenticationProvider.OpenIdConnect:
                var callbackUrl = Url.Page("/Index", null, null, Request.Scheme);
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    BlogAuthSchemas.OpenIdConnect);
            case AuthenticationProvider.Local:
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
                await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
                return RedirectToPage("/Index");
            default:
                return RedirectToPage("/Index");
        }
    }

    [AllowAnonymous]
    [HttpGet("/account/accessdenied")]
    [HttpGet("accessdenied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Content("Access Denied");
    }

    [Authorize(Policy = BlogAuthSchemas.AdministratorPolicy)]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new CurrentUserResponse(User.Identity?.Name ?? "Anonymous"));
    }

    [Authorize]
    [HttpGet("identity")]
    public IActionResult Identity()
    {
        if (_authenticationSettings.Provider != AuthenticationProvider.OpenIdConnect)
        {
            return NotFound();
        }

        var subjectClaim = User.FindFirst("sub");
        var issuer = User.FindFirst("iss")?.Value ?? subjectClaim?.Issuer;
        var subject = subjectClaim?.Value;

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject))
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                detail: "The OpenID Connect identity is missing its required issuer or subject claim.");
        }

        return Ok(new CurrentOidcIdentityResponse(
            issuer,
            subject,
            User.Identity?.Name ?? string.Empty));
    }
}

file sealed record CurrentUserResponse(string UserName);
file sealed record CurrentOidcIdentityResponse(string Issuer, string Subject, string DisplayName);
