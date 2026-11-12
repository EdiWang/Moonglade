using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Moonglade.Auth;

public static class LocalAccountPrincipal
{
    public const string RevisionClaim = "moonglade:account-revision";
    internal const string SessionClaim = "moonglade:local-session";

    public static ClaimsPrincipal Create(OwnerAccountSnapshot account, string scheme)
    {
        var identity = new ClaimsIdentity(scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, account.Email));
        identity.AddClaim(new Claim(RevisionClaim, account.Revision.ToString()));
        if (scheme == CookieAuthenticationDefaults.AuthenticationScheme)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrator"));
            identity.AddClaim(new Claim(SessionClaim, Guid.NewGuid().ToString("N")));
        }
        return new ClaimsPrincipal(identity);
    }

    public static bool Matches(ClaimsPrincipal principal, OwnerAccountSnapshot account) =>
        account?.IsLocalInitialized == true
        && Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == account.Id
        && Guid.TryParse(principal.FindFirstValue(RevisionClaim), out var revision) && revision == account.Revision;
}
