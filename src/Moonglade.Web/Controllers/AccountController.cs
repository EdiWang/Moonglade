using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;

namespace Moonglade.Web.Controllers;

[Route("api/account")]
public class AccountController(
    OwnerAccountStore accounts,
    IOptions<AuthenticationSettings> options,
    ICommandMediator commandMediator,
    ILogger<AccountController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet("password/generate")]
    public IActionResult GeneratePassword([FromServices] IPasswordGenerator passwordGenerator) =>
        options.Value.Provider != AuthenticationProvider.Local
            ? NotFound() : Ok(new GeneratedPasswordResponse(DateTime.UtcNow, passwordGenerator.GeneratePassword()));

    [HttpPut("password/local")]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
        if (options.Value.Provider != AuthenticationProvider.Local) return NotFound();
        var current = await accounts.ReadAsync(HttpContext.RequestAborted);
        if (!LocalAccountPrincipal.Matches(User, current)) return Unauthorized();
        OwnerAccountSnapshot saved;
        try
        {
            saved = await accounts.ChangePasswordAsync(current.Id, current.Revision,
                request.OldPassword, request.NewPassword, HttpContext.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The account changed. Sign in again.");
        }
        if (saved is null) return Conflict("Old password is incorrect.");
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            LocalAccountPrincipal.Create(saved, CookieAuthenticationDefaults.AuthenticationScheme));
        await LogActivityAsync(ActivityEvents.SettingsPasswordUpdated, saved.Email);
        return NoContent();
    }

    [HttpPut("totp/local/reset")]
    public async Task<IActionResult> ResetLocalAccountTotp(ResetLocalAccountTotpRequest request)
    {
        if (options.Value.Provider != AuthenticationProvider.Local) return NotFound();
        var current = await accounts.ReadAsync(HttpContext.RequestAborted);
        if (!LocalAccountPrincipal.Matches(User, current)) return Unauthorized();
        OwnerAccountSnapshot saved;
        try
        {
            saved = await accounts.ResetTotpAsync(current.Id, current.Revision, request.CurrentPassword, HttpContext.RequestAborted);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The account changed. Sign in again.");
        }
        if (saved is null) return Conflict("Current password is incorrect.");
        await LogActivityAsync(ActivityEvents.SettingsAuthenticatorReset, saved.Email);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        return NoContent();
    }
}

file sealed class GeneratedPasswordResponse(DateTime serverTimeUtc, string password)
{
    public DateTime ServerTimeUtc { get; } = serverTimeUtc;
    public string Password { get; } = password;
}
