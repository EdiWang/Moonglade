using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using System.Security.Claims;

namespace Moonglade.Auth;

public sealed class LocalAccountCookieEvents(LocalAccountStore accounts, IOptions<AuthenticationSettings> options, BlogDbContext db)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var account = await accounts.ReadAsync(principal, context.HttpContext.RequestAborted);
        if (account is not null && principal is not null && await IsValidSessionAsync(context, principal, account))
        {
            if (context.Scheme.Name == BlogAuthSchemas.Local && principal.Identity is ClaimsIdentity identity &&
                !identity.HasClaim(claim => claim.Type == LocalAccountPrincipal.SessionClaim))
            {
                identity.AddClaim(new(LocalAccountPrincipal.SessionClaim, Guid.NewGuid().ToString("N")));
                context.ShouldRenew = true;
            }
            return;
        }
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }

    private async Task<bool> IsValidSessionAsync(
        CookieValidatePrincipalContext context, ClaimsPrincipal principal, LocalAccountSnapshot account)
    {
        var scheme = context.Scheme.Name;
        var administrator = principal.IsInRole(BlogAuthSchemas.AdministratorRole);
        var collaborator = principal.IsInRole(BlogAuthSchemas.CollaboratorRole);
        var oidcRevision = principal.FindFirstValue(LocalAccountPrincipal.OidcSessionClaim);
        if (scheme != BlogAuthSchemas.Local && (administrator || collaborator || oidcRevision is not null)) return false;
        if (scheme == BlogAuthSchemas.TemporaryPasswordChange) return !account.IsOwner && account.MustChangePassword;
        if (account.MustChangePassword) return false;
        if (scheme == BlogAuthSchemas.LocalAccountRememberedDevice) return account.IsTotpEnabled;
        if (scheme != BlogAuthSchemas.Local) return true;

        if (administrator != account.IsOwner || collaborator == account.IsOwner) return false;
        if (oidcRevision is null) return !options.Value.Totp.Required || account.IsTotpEnabled;
        return account.IsOwner && Guid.TryParse(oidcRevision, out var revision) &&
            await db.OwnerOidc.AnyAsync(row => row.OwnerId == account.Id &&
                row.Issuer != null && row.Subject != null && row.SessionRevision == revision, context.HttpContext.RequestAborted);
    }
}
