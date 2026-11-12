using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;

namespace Moonglade.Web.Pages;

[AllowAnonymous]
[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class SetupAccountModel(
    IOptions<AuthenticationSettings> authSettings,
    OwnerAccountStore accounts,
    ILocalAccountTotpService totpService) : PageModel
{
    private const string PendingClaim = "moonglade:pending-setup";

    [BindProperty, Required, EmailAddress, StringLength(254)]
    [Display(Name = "Email")]
    public string Email { get; set; }

    [BindProperty, Required, DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [BindProperty]
    [Display(Name = "Authenticator code")]
    public string AuthenticatorCode { get; set; }

    public bool IsVerifying { get; private set; }
    public string SetupKey { get; private set; }
    public string QrCodeImageUri { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if ((await accounts.ReadAsync(HttpContext.RequestAborted))?.IsLocalInitialized == true)
            return RedirectToPage("/Index");
        var pending = await ReadPendingAsync();
        if (pending is not null) Display(pending);
        else Email = (await accounts.ReadProfileAsync(HttpContext.RequestAborted))?.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }
        try
        {
            var pending = await accounts.PrepareLocalSetupAsync(Email, Password, HttpContext.RequestAborted);
            Password = string.Empty;
            ModelState.Remove(nameof(Password));
            if (!pending.RequiresTotp)
                return await FinishAsync(await accounts.CompleteLocalSetupAsync(pending, null, HttpContext.RequestAborted));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(PendingClaim, JsonSerializer.Serialize(pending))], BlogAuthSchemas.InitialAccountSetup));
            await HttpContext.SignInAsync(BlogAuthSchemas.InitialAccountSetup, principal, new AuthenticationProperties
            {
                ExpiresUtc = new DateTimeOffset(pending.ExpiresUtc),
                AllowRefresh = false
            });
            return RedirectToPage("/SetupAccount");
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }
        catch (DbUpdateConcurrencyException)
        {
            return StatusCode(StatusCodes.Status409Conflict);
        }
    }

    public async Task<IActionResult> OnPostVerifyAsync()
    {
        ModelState.Remove(nameof(Email));
        ModelState.Remove(nameof(Password));
        var pending = await ReadPendingAsync();
        if (pending is null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            ModelState.AddModelError(string.Empty, "Account setup has expired. Start again.");
            return Page();
        }
        try
        {
            return await FinishAsync(await accounts.CompleteLocalSetupAsync(pending, AuthenticatorCode, HttpContext.RequestAborted));
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), exception.Message);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            Display(pending);
            return Page();
        }
        catch (DbUpdateConcurrencyException)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
            return StatusCode(StatusCodes.Status409Conflict);
        }
    }

    private async Task<PendingOwnerSetup> ReadPendingAsync()
    {
        var ticket = await HttpContext.AuthenticateAsync(BlogAuthSchemas.InitialAccountSetup);
        var json = ticket.Principal?.FindFirstValue(PendingClaim);
        if (!ticket.Succeeded || string.IsNullOrEmpty(json)) return null;
        PendingOwnerSetup pending;
        try
        {
            pending = JsonSerializer.Deserialize<PendingOwnerSetup>(json);
        }
        catch (JsonException)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
            return null;
        }
        var account = await accounts.ReadAsync(HttpContext.RequestAborted);
        if (pending is null || account?.IsLocalInitialized == true || account?.Revision != pending.ExpectedRevision
            || pending.RequiresTotp != authSettings.Value.Totp.Required)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
            return null;
        }
        return pending;
    }

    private void Display(PendingOwnerSetup pending)
    {
        IsVerifying = true;
        SetupKey = pending.TotpSecret;
        QrCodeImageUri = AuthenticatorQrCode.Create(
            totpService.BuildAuthenticatorUri(authSettings.Value.Totp.Issuer, pending.Email, pending.TotpSecret));
    }

    private async Task<IActionResult> FinishAsync(OwnerAccountSnapshot account)
    {
        await HttpContext.SignOutAsync(BlogAuthSchemas.InitialAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountTwoFactor);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            LocalAccountPrincipal.Create(account, CookieAuthenticationDefaults.AuthenticationScheme));
        return RedirectToPage("/Index");
    }
}
