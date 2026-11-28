using Microsoft.AspNetCore.Authentication;

namespace Moonglade.Web.Services;

internal static class LocalAccountSignInHttpExtensions
{
    public static async Task<string> SignInToStepAsync(
        this HttpContext context, LocalAccountSnapshot account, LocalAccountSignInStep step)
    {
        var (scheme, page) = step switch
        {
            LocalAccountSignInStep.ChangeTemporaryPassword => (BlogAuthSchemas.TemporaryPasswordChange, "/ChangeTemporaryPassword"),
            LocalAccountSignInStep.EnrollAuthenticator => (BlogAuthSchemas.LocalAccountSetup, "/SetupAuthenticator"),
            LocalAccountSignInStep.VerifyAuthenticator => (BlogAuthSchemas.LocalAccountTwoFactor, "/VerifyAuthenticator"),
            LocalAccountSignInStep.Complete => (BlogAuthSchemas.Local, account.IsOwner ? "/Admin/Dashboard" : "/Admin/Post"),
            _ => throw new ArgumentOutOfRangeException(nameof(step), step, null)
        };
        await context.SignInAsync(scheme, LocalAccountPrincipal.Create(account, scheme));
        return page;
    }
}
