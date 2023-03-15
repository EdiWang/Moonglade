using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Moonglade.Web.Controllers;

[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationSettings _authenticationSettings;

    public AuthController(
        IOptions<AuthenticationSettings> authSettings) =>
        _authenticationSettings = authSettings.Value;

    [HttpGet("signout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> SignOut(int nounce = 1055)
    {
        switch (_authenticationSettings.Provider)
        {
            case AuthenticationProvider.AzureAD:
                var callbackUrl = Url.Page("/Index", null, null, Request.Scheme);
                return SignOut(
                    new AuthenticationProperties { RedirectUri = callbackUrl },
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    OpenIdConnectDefaults.AuthenticationScheme);
            case AuthenticationProvider.Local:
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Index");
            default:
                return RedirectToPage("/Index");
        }
    }

    [AllowAnonymous]
    [HttpGet("/account/accessdenied")]
    [HttpGet("accessdenied")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Content("Access Denied");
    }
}