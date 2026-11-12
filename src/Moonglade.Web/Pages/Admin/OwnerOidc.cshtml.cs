#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Moonglade.Auth.Oidc;
using Moonglade.Web.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Moonglade.Web.Pages.Admin;

[EnableRateLimiting(LocalAccountRateLimitPolicy.PolicyName)]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class OwnerOidcModel(OwnerOidcStore store, OwnerAccountStore accounts, OidcBindingFlow flow,
    IOptions<AuthenticationSettings> authentication, IStringLocalizer<Program> localizer, ILogger<OwnerOidcModel> logger) : PageModel
{
    [BindProperty] public Guid? Revision { get; set; }
    [BindProperty] public string? DisplayName { get; set; }
    [BindProperty] public string? Authority { get; set; }
    [BindProperty] public string? ClientId { get; set; }
    [BindProperty] public string? ClientSecret { get; set; }
    [BindProperty] public string? CurrentPassword { get; set; }
    [BindProperty] public string? AuthenticatorCode { get; set; }
    [BindProperty] public bool ConfirmDelete { get; set; }

    public OwnerOidcSettings? Settings { get; private set; }
    public PendingExternalAccount? Pending { get; private set; }
    public bool CanReuseProof { get; private set; }
    public bool RequireTotp { get; private set; }
    public bool Failed { get; private set; }
    public string CallbackUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase.Add(OidcBindingFlow.CallbackPath)}";

    private Guid OwnerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private CancellationToken Ct => HttpContext.RequestAborted;

    public async Task OnGetAsync(bool failed = false)
    {
        Failed = failed;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid) return await RejectAsync("Enter valid provider settings.");
        if (!await VerifyProofAsync()) return await RejectAsync("Verify your local password and applicable authenticator code.");
        try
        {
            var revision = await store.SaveAsync(OwnerId, Revision, DisplayName, Authority, ClientId, ClientSecret, Ct);
            var proof = flow.CreateProof(User, revision);
            if (proof is not null) flow.SaveProof(HttpContext, proof);
            await HttpContext.SignOutAsync(OidcBindingFlow.PendingScheme);
            return RedirectToPage();
        }
        catch (ValidationException ex)
        {
            return await RejectAsync(ex.Message);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning("OIDC settings could not be saved ({ErrorType}).", ex.GetType().Name);
            return await RejectAsync("The provider settings could not be saved. Reload the page and try again.");
        }
        catch (CryptographicException)
        {
            logger.LogWarning("The OIDC client secret could not be protected.");
            return await RejectAsync("The provider settings could not be saved. Reload the page and try again.");
        }
    }

    public async Task<IActionResult> OnPostBindAsync()
    {
        var settings = await store.ReadAsync(Ct);
        if (!ModelState.IsValid || settings is not { Enabled: true, AccountLabel: null } || settings.Revision != Revision)
            return await RejectAsync("The provider settings changed. Reload the page and try again.");

        var proof = flow.ReadSavedProof(HttpContext);
        if (!flow.Matches(User, settings.Revision, proof))
        {
            if (!await VerifyProofAsync()) return await RejectAsync("Verify your local password and applicable authenticator code.");
            proof = flow.CreateProof(User, settings.Revision);
        }

        if (proof is null) return await RejectAsync("The binding request expired. Try again.");
        flow.ClearProof(HttpContext);
        await HttpContext.SignOutAsync(OidcBindingFlow.PendingScheme);
        var properties = new AuthenticationProperties { RedirectUri = Url.Page("/Admin/OwnerOidc") };
        properties.Items[OidcBindingFlow.ProofKey] = proof;
        return Challenge(properties, OidcBindingFlow.BindingScheme);
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        var settings = await store.ReadAsync(Ct);
        var pending = await ReadPendingAsync(settings);
        if (settings is null || pending is null || settings.Revision != Revision)
            return await RejectAsync("The binding request expired. Try again.");

        try
        {
            await store.BindAsync(OwnerId, settings.Revision, pending.Issuer, pending.Subject, pending.Label, Ct);
            return await OnPostCancelAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RejectAsync("The provider settings changed. Reload the page and try again.");
        }
        catch (ValidationException ex)
        {
            return await RejectAsync(ex.Message);
        }
    }

    public async Task<IActionResult> OnPostCancelAsync()
    {
        flow.ClearProof(HttpContext);
        await HttpContext.SignOutAsync(OidcBindingFlow.PendingScheme);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostManageAsync(string operation)
    {
        if (!ModelState.IsValid || Revision is not { } revision)
            return await RejectAsync("The provider settings changed. Reload the page and try again.");
        if (!await VerifyProofAsync()) return await RejectAsync("Verify your local password and applicable authenticator code.");

        try
        {
            switch (operation)
            {
                case "enable":
                case "disable":
                    await store.SetEnabledAsync(OwnerId, revision, operation == "enable", Ct);
                    break;
                case "unbind":
                    await store.UnbindAsync(OwnerId, revision, Ct);
                    break;
                case "delete" when ConfirmDelete:
                    await store.DeleteAsync(OwnerId, revision, Ct);
                    break;
                default:
                    return await RejectAsync("Confirm deletion before deleting the provider.");
            }
            return await OnPostCancelAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RejectAsync("The provider settings changed. Reload the page and try again.");
        }
    }

    private Task<bool> VerifyProofAsync() => accounts.VerifyManagementProofAsync(User, CurrentPassword, AuthenticatorCode, Ct);

    private async Task<IActionResult> RejectAsync(string error)
    {
        ModelState.Clear();
        ModelState.AddModelError(string.Empty, localizer[error]);
        ClientSecret = CurrentPassword = AuthenticatorCode = null;
        await LoadAsync();
        return Page();
    }

    private async Task LoadAsync()
    {
        Settings = await store.ReadAsync(Ct);
        Revision = Settings?.Revision;
        DisplayName = Settings?.DisplayName;
        Authority = Settings?.Authority;
        ClientId = Settings?.ClientId;
        RequireTotp = authentication.Value.Totp.Required || (await accounts.ReadAsync(Ct))?.IsTotpEnabled == true;
        CanReuseProof = Settings is { Enabled: true, AccountLabel: null } &&
            flow.Matches(User, Settings.Revision, flow.ReadSavedProof(HttpContext));
        Pending = await ReadPendingAsync(Settings);
    }

    private async Task<PendingExternalAccount?> ReadPendingAsync(OwnerOidcSettings? settings)
    {
        if (settings is not { Enabled: true, AccountLabel: null }) return null;
        var ticket = await HttpContext.AuthenticateAsync(OidcBindingFlow.PendingScheme);
        var state = ticket.Properties?.Items;
        return ticket.Succeeded && state is not null &&
            state.TryGetValue(OidcBindingFlow.ProofKey, out var proof) && flow.Matches(User, settings.Revision, proof) &&
            state.TryGetValue(OidcBindingFlow.IssuerKey, out var issuer) && issuer is not null &&
            state.TryGetValue(OidcBindingFlow.SubjectKey, out var subject) && subject is not null &&
            state.TryGetValue(OidcBindingFlow.LabelKey, out var label) && label is not null
            ? new PendingExternalAccount(issuer, subject, label)
            : null;
    }

    public sealed record PendingExternalAccount(string Issuer, string Subject, string Label);
}
