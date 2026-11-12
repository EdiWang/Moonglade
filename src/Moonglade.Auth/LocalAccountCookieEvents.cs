using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace Moonglade.Auth;

public sealed class LocalAccountCookieEvents(OwnerAccountStore accounts, IOptions<AuthenticationSettings> options)
    : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var account = await accounts.ReadAsync(context.HttpContext.RequestAborted);
        var applicationCookie = context.Scheme.Name == CookieAuthenticationDefaults.AuthenticationScheme;
        if (LocalAccountPrincipal.Matches(context.Principal, account)
            && (!applicationCookie || !options.Value.Totp.Required || account.IsTotpEnabled))
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
}
