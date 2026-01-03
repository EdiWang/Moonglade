using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Controllers;

[Route("auth")]
public class AuthController(
    IOptions<AuthenticationSettings> authSettings,
    IQueryMediator queryMediator
    ) : ControllerBase
{
    private readonly AuthenticationSettings _authenticationSettings = authSettings.Value;

    [HttpGet("signout")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> SignOut(int nounce = 996)
    {
        switch (_authenticationSettings.Provider)
        {
            case AuthenticationProvider.EntraID:
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

    [Authorize]
    [HttpGet("loginhistory/list")]
    [ProducesResponseType<List<LoginHistoryEntity>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLoginHistory()
    {
        var data = await queryMediator.QueryAsync(new ListLoginHistoryQuery());
        return Ok(data);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        return Ok(new { UserName = User.Identity?.Name ?? "Anonymous" });
    }
}