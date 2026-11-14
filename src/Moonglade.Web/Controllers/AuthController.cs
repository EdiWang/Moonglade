using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Moonglade.Web.Controllers;

[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpGet("signout")]
    public async Task<IActionResult> SignOut(int nounce = 996)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        return RedirectToPage("/Index");
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
}

file sealed record CurrentUserResponse(string UserName);
