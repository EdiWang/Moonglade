using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moonglade.Data;
using System.Security.Claims;

namespace Moonglade.Auth;

public sealed class LocalAccountCookieEvents(OwnerAccountStore accounts, IOptions<AuthenticationSettings> options, BlogDbContext db)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var account = await accounts.ReadAsync(context.HttpContext.RequestAborted);
        var applicationCookie = context.Scheme.Name == CookieAuthenticationDefaults.AuthenticationScheme;
        if (LocalAccountPrincipal.Matches(context.Principal, account)
            && (context.Scheme.Name != BlogAuthSchemas.LocalAccountRememberedDevice || account.IsTotpEnabled)
            && (!applicationCookie || await IsApplicationSessionValidAsync(context, account)))
        {
            if (applicationCookie && context.Principal.Identity is System.Security.Claims.ClaimsIdentity identity &&
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

    private Task<bool> IsApplicationSessionValidAsync(CookieValidatePrincipalContext context, OwnerAccountSnapshot account)
    {
        var oidcRevision = context.Principal.FindFirstValue(LocalAccountPrincipal.OidcSessionClaim);
        if (oidcRevision is null) return Task.FromResult(!options.Value.Totp.Required || account.IsTotpEnabled);
        if (!Guid.TryParse(oidcRevision, out var revision)) return Task.FromResult(false);

        return db.OwnerOidc.AnyAsync(row => row.OwnerId == account.Id && row.Enabled &&
            row.Issuer != null && row.Subject != null && row.SessionRevision == revision, context.HttpContext.RequestAborted);
    }
}
