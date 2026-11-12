#nullable enable

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace Moonglade.Auth.Oidc;

public sealed class OidcBindingFlow(IDataProtectionProvider protection)
{
    public const string PendingScheme = "MoongladeOidcPendingBinding";
    public const string BindingScheme = "MoongladeOidcBinding";
    public const string CallbackPath = "/signin-oidc";
    public const string ManagementPath = "/admin/account/oidc";
    public const string ProofKey = "moonglade:oidc-binding-proof";
    public const string IssuerKey = "moonglade:oidc-issuer";
    public const string SubjectKey = "moonglade:oidc-subject";
    public const string LabelKey = "moonglade:oidc-label";
    private const string ProofCookie = ".Moonglade.OidcBindingProof";
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private readonly ITimeLimitedDataProtector _proof = protection.CreateProtector("OidcBindingProof.v1").ToTimeLimitedDataProtector();

    public string? CreateProof(ClaimsPrincipal principal, Guid revision)
    {
        var session = principal.FindFirstValue(LocalAccountPrincipal.SessionClaim);
        var ownerId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var accountRevision = principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim);
        return session is not null && ownerId is not null && accountRevision is not null
            ? _proof.Protect(JsonSerializer.Serialize(new BindingProof(ownerId, accountRevision, revision, session)), Lifetime)
            : null;
    }

    public bool Matches(ClaimsPrincipal principal, Guid revision, string? proof)
    {
        if (proof is null || principal.Identity?.IsAuthenticated != true) return false;
        try
        {
            var data = JsonSerializer.Deserialize<BindingProof>(_proof.Unprotect(proof));
            return data is not null && data.Revision == revision
                && data.SessionId == principal.FindFirstValue(LocalAccountPrincipal.SessionClaim)
                && data.OwnerId == principal.FindFirstValue(ClaimTypes.NameIdentifier)
                && data.AccountRevision == principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public string? ReadSavedProof(HttpContext context) => context.Request.Cookies[ProofCookie];

    public void SaveProof(HttpContext context, string proof) =>
        context.Response.Cookies.Append(ProofCookie, proof, CookieOptions(context));

    public void ClearProof(HttpContext context) =>
        context.Response.Cookies.Delete(ProofCookie, CookieOptions(context));

    public async Task<ChallengeResult> StartBindingAsync(HttpContext context, string proof)
    {
        ClearProof(context);
        await context.SignOutAsync(PendingScheme);
        var properties = new AuthenticationProperties();
        properties.Items[ProofKey] = proof;
        return new ChallengeResult(BindingScheme, properties);
    }

    private static CookieOptions CookieOptions(HttpContext context) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = context.Request.PathBase.Add(ManagementPath),
        MaxAge = Lifetime,
        IsEssential = true
    };

    private sealed record BindingProof(string OwnerId, string AccountRevision, Guid Revision, string SessionId);
}
