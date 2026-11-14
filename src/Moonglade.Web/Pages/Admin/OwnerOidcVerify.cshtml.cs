#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Services;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Moonglade.Web.Pages.Admin;

[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class OwnerOidcVerifyModel(OwnerOidcStore store, OwnerAccountStore accounts, OidcSetupDrafts drafts, OidcBindingFlow flow,
    IOptions<AuthenticationSettings> authentication, IStringLocalizer<Program> localizer, ILogger<OwnerOidcVerifyModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Draft { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? Revision { get; set; }
    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? AuthenticatorCode { get; set; }

    public bool RequireTotp { get; private set; }
    private CancellationToken Ct => HttpContext.RequestAborted;

    public async Task<IActionResult> OnGetAsync() =>
        await CanVerifyAsync() ? await ShowAsync() : Expired();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await CanVerifyAsync()) return Expired();
        if (!await accounts.VerifyManagementProofAsync(User, CurrentPassword, AuthenticatorCode, Ct))
            return await ShowAsync("Verify your local password and applicable authenticator code.");

        if (Draft is not { } draftId)
        {
            var proof = flow.CreateProof(User, Revision!.Value);
            return proof is not null ? await flow.StartBindingAsync(HttpContext, proof) : Expired();
        }

        var input = drafts.Read(User, draftId);
        if (input is null) return Expired();
        try
        {
            var ownerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var revision = await store.SaveAsync(ownerId, null, input.DisplayName, input.Authority, input.ClientId, input.ClientSecret, Ct);
            drafts.Clear(User, draftId);
            var proof = flow.CreateProof(User, revision);
            if (proof is not null) flow.SaveProof(HttpContext, proof);
            await HttpContext.SignOutAsync(OidcBindingFlow.PendingScheme);
            return RedirectToPage("/Admin/OwnerOidc");
        }
        catch (Exception ex) when (ex is DbUpdateException or CryptographicException)
        {
            logger.LogWarning("OIDC settings could not be saved ({ErrorType}).", ex.GetType().Name);
            return await ShowAsync("The provider settings could not be saved. Reload the page and try again.");
        }
    }

    public IActionResult OnPostCancel()
    {
        if (Draft is { } draft) drafts.Clear(User, draft);
        return RedirectToPage("/Admin/OwnerOidc");
    }

    private async Task<bool> CanVerifyAsync()
    {
        if (!ModelState.IsValid || Draft.HasValue == Revision.HasValue) return false;
        var settings = await store.ReadAsync(Ct);
        return Draft is { } draft
            ? settings is null && drafts.Read(User, draft) is not null
            : settings is { Enabled: true, AccountLabel: null } && settings.Revision == Revision;
    }

    private async Task<PageResult> ShowAsync(string? error = null)
    {
        if (error is not null) ModelState.AddModelError(string.Empty, localizer[error]);
        CurrentPassword = AuthenticatorCode = null;
        RequireTotp = authentication.Value.Totp.Required || (await accounts.ReadAsync(Ct))?.IsTotpEnabled == true;
        return Page();
    }

    private RedirectToPageResult Expired() =>
        RedirectToPage("/Admin/OwnerOidc", new RouteValueDictionary { ["expired"] = true });
}
