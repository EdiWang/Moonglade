using Edi.PasswordGenerator;
using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Moonglade.ActivityLog;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/account")]
public class AccountController(
    OwnerAccountStore accounts,
    ICommandMediator commandMediator,
    ILogger<AccountController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(OwnerPresentation profile, [FromServices] OwnerAvatarStore store,
        [FromServices] PostCacheInvalidator cache, CancellationToken ct)
    {
        if (await store.SetPresentationAsync(profile, ct))
        {
            cache.InvalidateFeeds();
            await LogActivityAsync(ActivityEvents.AccountProfileUpdated, null);
        }

        return NoContent();
    }

    [HttpGet("email")]
    public async Task<IActionResult> GetEmail()
    {
        var account = await accounts.ReadAsync(HttpContext.RequestAborted);
        return Ok(new OwnerEmailResponse(account?.Email, account?.Revision));
    }

    [HttpPut("email")]
    public async Task<IActionResult> UpdateEmail(UpdateOwnerEmailRequest request)
    {
        var current = await accounts.ReadAsync(HttpContext.RequestAborted);
        if (!LocalAccountPrincipal.Matches(User, current)) return Unauthorized();
        if (request.Revision != current?.Revision) return Conflict("The account changed. Reload the account page.");
        OwnerAccountSnapshot saved;
        try
        {
            saved = await accounts.ChangeLocalEmailAsync(current.Id, current.Revision,
                request.CurrentPassword, request.Email, HttpContext.RequestAborted);
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(nameof(request.Email), exception.Message);
            return ValidationProblem(ModelState);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("The account changed. Reload the account page.");
        }
        if (saved is null) return Conflict("Current password is incorrect.");
        if (saved.Revision != current.Revision)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
            await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
            await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
        }
        return NoContent();
    }

    [HttpGet("password/generate")]
    public IActionResult GeneratePassword([FromServices] IPasswordGenerator passwordGenerator) =>
        Ok(new GeneratedPasswordResponse(DateTime.UtcNow, passwordGenerator.GeneratePassword()));

    [HttpPut("password/local")]
    public async Task<IActionResult> UpdateLocalAccountPassword(UpdateLocalAccountPasswordRequest request)
    {
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
            LocalAccountPrincipal.Refresh(saved, User));
        await LogActivityAsync(ActivityEvents.SettingsPasswordUpdated, saved.Email);
        return NoContent();
    }

    [HttpPut("totp/local/reset")]
    public async Task<IActionResult> ResetLocalAccountTotp(ResetLocalAccountTotpRequest request)
    {
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

file sealed record OwnerEmailResponse(string Email, Guid? Revision);

file sealed class GeneratedPasswordResponse(DateTime serverTimeUtc, string password)
{
    public DateTime ServerTimeUtc { get; } = serverTimeUtc;
    public string Password { get; } = password;
}
