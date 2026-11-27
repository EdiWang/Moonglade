#nullable enable

using System.Security.Claims;

namespace Moonglade.Auth;

public static class LocalAccountPrincipal
{
    public const string RevisionClaim = "moonglade:account-revision";
    internal const string SessionClaim = "moonglade:local-session";
    internal const string OidcSessionClaim = "moonglade:oidc-session-revision";

    public static ClaimsPrincipal Create(OwnerAccountSnapshot account, string scheme, Guid? oidcSessionRevision = null) =>
        Create(account.Id, account.Email, account.Revision, BlogAuthSchemas.AdministratorRole, null, scheme, oidcSessionRevision);

    public static ClaimsPrincipal Create(LocalAccountSnapshot account, string scheme) =>
        Create(account.Id, account.Email, account.Revision,
            account.IsOwner ? BlogAuthSchemas.AdministratorRole : BlogAuthSchemas.CollaboratorRole,
            account.DisplayName, scheme, null);

    private static ClaimsPrincipal Create(Guid id, string email, Guid revision, string role, string? displayName,
        string scheme, Guid? oidcSessionRevision)
    {
        var identity = new ClaimsIdentity(scheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, email));
        identity.AddClaim(new Claim(RevisionClaim, revision.ToString()));
        if (scheme == BlogAuthSchemas.Local)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            identity.AddClaim(new Claim(SessionClaim, Guid.NewGuid().ToString("N")));
            if (!string.IsNullOrWhiteSpace(displayName))
                identity.AddClaim(new Claim(BlogAuthSchemas.AccountDisplayNameClaim, displayName));
            if (oidcSessionRevision is { } oidcRevision)
                identity.AddClaim(new Claim(OidcSessionClaim, oidcRevision.ToString()));
        }
        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal Refresh(OwnerAccountSnapshot account, ClaimsPrincipal current) =>
        Create(account, BlogAuthSchemas.Local,
            current.FindFirstValue(OidcSessionClaim) is { } origin ? Guid.Parse(origin) : null);

    public static bool Matches(ClaimsPrincipal? principal, OwnerAccountSnapshot? account) =>
        account?.IsLocalInitialized == true && MatchesRevision(principal, account.Id, account.Revision);

    public static bool MatchesAccount(ClaimsPrincipal? principal, LocalAccountSnapshot account) =>
        !account.IsDisabled && MatchesRevision(principal, account.Id, account.Revision);

    private static bool MatchesRevision(ClaimsPrincipal? principal, Guid accountId, Guid accountRevision) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id == accountId
        && Guid.TryParse(principal?.FindFirstValue(RevisionClaim), out var revision) && revision == accountRevision;
}
