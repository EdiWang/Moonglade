#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Moonglade.Auth;

public enum LocalAccountSignInStep
{
    ChangeTemporaryPassword,
    EnrollAuthenticator,
    VerifyAuthenticator,
    Complete
}

public static class LocalAccountSignIn
{
    public static LocalAccountSignInStep AfterPassword(LocalAccountSnapshot account, bool totpRequired)
    {
        if (account.MustChangePassword) return LocalAccountSignInStep.ChangeTemporaryPassword;
        if (RequiresAuthenticatorEnrollment(account, totpRequired)) return LocalAccountSignInStep.EnrollAuthenticator;
        return totpRequired ? LocalAccountSignInStep.VerifyAuthenticator : LocalAccountSignInStep.Complete;
    }

    // Completing a temporary password change already verifies an enrolled authenticator when required.
    public static LocalAccountSignInStep AfterTemporaryPasswordChange(LocalAccountSnapshot account, bool totpRequired) =>
        RequiresAuthenticatorEnrollment(account, totpRequired)
            ? LocalAccountSignInStep.EnrollAuthenticator
            : LocalAccountSignInStep.Complete;

    public static bool CanChangeTemporaryPassword([NotNullWhen(true)] LocalAccountSnapshot? account) =>
        account is { IsOwner: false, MustChangePassword: true };

    internal static bool RequiresAuthenticatorEnrollment(LocalAccountSnapshot account, bool totpRequired) =>
        totpRequired && !account.IsTotpEnabled;
}
