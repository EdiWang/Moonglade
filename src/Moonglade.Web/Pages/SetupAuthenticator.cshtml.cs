using LiteBus.Commands.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QRCoder;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Moonglade.Web.Pages;

[Authorize(AuthenticationSchemes = BlogAuthSchemas.LocalAccountSetup)]
public class SetupAuthenticatorModel(
    IOptions<AuthenticationSettings> authSettings,
    IBlogConfig blogConfig,
    ICommandMediator commandMediator,
    ILocalAccountTotpService totpService,
    ILogger<SetupAuthenticatorModel> logger) : PageModel
{
    private readonly AuthenticationSettings _authenticationSettings = authSettings.Value;

    public string SetupKey { get; private set; }
    public string QrCodeImageUri { get; private set; }

    [BindProperty]
    [Required]
    [Display(Name = "Authenticator code")]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression("[0-9]{6}")]
    public string AuthenticatorCode { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (_authenticationSettings.Provider != AuthenticationProvider.Local)
        {
            return RedirectToPage("/Index");
        }

        var account = blogConfig.LocalAccountSettings;
        if (IsTotpConfigured(account))
        {
            return RedirectToPage("/SignIn");
        }

        await EnsureTotpSecretAsync(account);
        BuildAuthenticatorDisplay(account);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (_authenticationSettings.Provider != AuthenticationProvider.Local)
        {
            return RedirectToPage("/Index");
        }

        var account = blogConfig.LocalAccountSettings;
        if (IsTotpConfigured(account))
        {
            return RedirectToPage("/SignIn");
        }

        await EnsureTotpSecretAsync(account);

        if (!ModelState.IsValid)
        {
            BuildAuthenticatorDisplay(account);
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Page();
        }

        if (!totpService.VerifyCode(account.TotpSecret, AuthenticatorCode))
        {
            ModelState.AddModelError(nameof(AuthenticatorCode), "Invalid authenticator code.");
            BuildAuthenticatorDisplay(account);
            return Page();
        }

        account.IsTotpEnabled = true;
        await SaveLocalAccountSettingsAsync(account);
        await HttpContext.SignOutAsync(BlogAuthSchemas.LocalAccountSetup);
        await SignInAdminAsync(account.Username);

        logger.LogInformation("TOTP authenticator setup completed for local account '{Username}'", account.Username);

        return RedirectToPage("/Admin/Dashboard");
    }

    private async Task EnsureTotpSecretAsync(LocalAccountSettings account)
    {
        if (!string.IsNullOrWhiteSpace(account.TotpSecret))
        {
            return;
        }

        account.TotpSecret = totpService.GenerateSecret();
        account.IsTotpEnabled = false;
        await SaveLocalAccountSettingsAsync(account);
    }

    private void BuildAuthenticatorDisplay(LocalAccountSettings account)
    {
        SetupKey = account.TotpSecret;
        var issuer = _authenticationSettings.Totp.Issuer;
        var authenticatorUri = totpService.BuildAuthenticatorUri(issuer, account.Username, account.TotpSecret);
        QrCodeImageUri = CreateQrCodeImageUri(authenticatorUri);
    }

    private async Task SaveLocalAccountSettingsAsync(LocalAccountSettings account)
    {
        var kvp = blogConfig.UpdateAsync(account);
        await commandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
    }

    private async Task SignInAdminAsync(string username)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, "Administrator")
        };
        var ci = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(ci);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    private static bool IsTotpConfigured(LocalAccountSettings account) =>
        account.IsTotpEnabled && !string.IsNullOrWhiteSpace(account.TotpSecret);

    private static string CreateQrCodeImageUri(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(authenticatorUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(8);

        return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
    }
}
