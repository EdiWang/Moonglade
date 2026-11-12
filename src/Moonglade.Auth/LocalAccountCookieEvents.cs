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
        if (LocalAccountPrincipal.Matches(context.Principal, account)
            && (context.Scheme.Name != CookieAuthenticationDefaults.AuthenticationScheme
                || !options.Value.Totp.Required || account.IsTotpEnabled)) return;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
