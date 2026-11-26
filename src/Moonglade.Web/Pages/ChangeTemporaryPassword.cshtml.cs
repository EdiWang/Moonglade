#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Pages;

[Authorize(AuthenticationSchemes = BlogAuthSchemas.TemporaryPasswordChange)]
[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
public class ChangeTemporaryPasswordModel(LocalAccountStore accounts, IOptions<AuthenticationSettings> settings,
    IStringLocalizer<Program> localizer) : PageModel
{
    [BindProperty, Required, DataType(DataType.Password)]
    [RegularExpression(UpdateLocalAccountPasswordRequest.PasswordPattern, ErrorMessage = UpdateLocalAccountPasswordRequest.PasswordRequirements)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty, Required, DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty, RegularExpression("[0-9]{6}")]
    public string? AuthenticatorCode { get; set; }

    public bool RequiresAuthenticatorCode { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var account = await accounts.ReadAsync(User, HttpContext.RequestAborted);
        if (account is not { IsOwner: false, MustChangePassword: true }) return RedirectToPage("/SignIn");
        RequiresAuthenticatorCode = settings.Value.Totp.Required && account.IsTotpEnabled;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var account = await accounts.ReadAsync(User, HttpContext.RequestAborted);
        if (account is not { IsOwner: false, MustChangePassword: true }) return RedirectToPage("/SignIn");
        RequiresAuthenticatorCode = settings.Value.Totp.Required && account.IsTotpEnabled;
        if (!ModelState.IsValid)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        LocalAccountSnapshot? saved;
        try
        {
            saved = await accounts.CompleteTemporaryPasswordChangeAsync(account.Id, account.Revision,
                NewPassword, AuthenticatorCode, HttpContext.RequestAborted);
        }
        catch (ValidationException exception)
        {
            ModelState.AddModelError(nameof(NewPassword), localizer[exception.Message].Value);
            return Page();
        }
        catch (DbUpdateConcurrencyException)
        {
            await HttpContext.SignOutAsync(BlogAuthSchemas.TemporaryPasswordChange);
            return RedirectToPage("/SignIn");
        }
        if (saved is null)
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), localizer["Invalid authenticator code."].Value);
            return Page();
        }

        await HttpContext.SignOutAsync(BlogAuthSchemas.TemporaryPasswordChange);
        var scheme = settings.Value.Totp.Required && !saved.IsTotpEnabled
            ? BlogAuthSchemas.LocalAccountSetup
            : BlogAuthSchemas.Local;
        await HttpContext.SignInAsync(scheme, LocalAccountPrincipal.Create(saved, scheme));
        return RedirectToPage(scheme == BlogAuthSchemas.Local ? "/Admin/Post" : "/SetupAuthenticator");
    }
}
