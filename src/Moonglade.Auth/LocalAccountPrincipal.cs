using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Moonglade.Auth;

public static class LocalAccountPrincipal
{
    public const string RevisionClaim = "moonglade:account-revision";
    internal const string SessionClaim = "moonglade:local-session";
    internal const string OidcSessionClaim = "moonglade:oidc-session-revision";

    public static ClaimsPrincipal Create(OwnerAccountSnapshot account, string scheme, Guid? oidcSessionRevision = null)
    {
        var identity = new ClaimsIdentity(scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, account.Email));
        identity.AddClaim(new Claim(RevisionClaim, account.Revision.ToString()));
        if (scheme == CookieAuthenticationDefaults.AuthenticationScheme)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrator"));
            identity.AddClaim(new Claim(SessionClaim, Guid.NewGuid().ToString("N")));
            if (oidcSessionRevision is { } revision)
                identity.AddClaim(new Claim(OidcSessionClaim, revision.ToString()));
        }
        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal Refresh(OwnerAccountSnapshot account, ClaimsPrincipal current) =>
        Create(account, BlogAuthSchemas.Local,
            current.FindFirstValue(OidcSessionClaim) is { } origin ? Guid.Parse(origin) : null);

    public static bool Matches(ClaimsPrincipal principal, OwnerAccountSnapshot account) =>
        account?.IsLocalInitialized == true
        && Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == account.Id
        && Guid.TryParse(principal.FindFirstValue(RevisionClaim), out var revision) && revision == account.Revision;
}
