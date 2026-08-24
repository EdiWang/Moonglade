using Microsoft.Extensions.Options;

namespace Moonglade.Auth;

public class AuthenticationSettingsValidator : IValidateOptions<AuthenticationSettings>
{
    public ValidateOptionsResult Validate(string name, AuthenticationSettings options)
    {
        if (options.Provider == AuthenticationProvider.Local)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.Provider != AuthenticationProvider.OpenIdConnect)
        {
            return ValidateOptionsResult.Fail(
                $"Authentication provider '{options.Provider}' is not supported. Supported values: Local, OpenIdConnect.");
        }

        if (options.OpenIdConnect is null)
        {
            return ValidateOptionsResult.Fail(
                "Authentication:OpenIdConnect is required when Authentication:Provider is OpenIdConnect.");
        }

        var oidc = options.OpenIdConnect;
        var failures = new List<string>();

        if (!IsValidHttpsAuthority(oidc.Authority))
        {
            failures.Add("Authentication:OpenIdConnect:Authority must be an absolute HTTPS URL without a query or fragment.");
        }

        AddRequiredFailure(oidc.ClientId, "ClientId", failures);
        AddRequiredFailure(oidc.ClientSecret, "ClientSecret", failures);
        AddRequiredFailure(oidc.NameClaimType, "NameClaimType", failures);

        if (!IsValidCallbackPath(oidc.CallbackPath))
        {
            failures.Add("Authentication:OpenIdConnect:CallbackPath must be an application-relative path that starts with a single '/'.");
        }

        if (!IsValidCallbackPath(oidc.SignedOutCallbackPath))
        {
            failures.Add("Authentication:OpenIdConnect:SignedOutCallbackPath must be an application-relative path that starts with a single '/'.");
        }

        if (oidc.Scopes is not { Length: > 0 } ||
            oidc.Scopes.Any(string.IsNullOrWhiteSpace) ||
            !oidc.Scopes.Contains("openid", StringComparer.Ordinal))
        {
            failures.Add("Authentication:OpenIdConnect:Scopes must contain 'openid' and cannot contain blank values.");
        }

        if (oidc.AllowedSubjects is null || oidc.AllowedSubjects.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("Authentication:OpenIdConnect:AllowedSubjects cannot contain blank OIDC subject identifiers.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsValidHttpsAuthority(string authority) =>
        Uri.TryCreate(authority, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps &&
        !string.IsNullOrWhiteSpace(uri.Host) &&
        string.IsNullOrEmpty(uri.Query) &&
        string.IsNullOrEmpty(uri.Fragment);

    private static bool IsValidCallbackPath(string path) =>
        !string.IsNullOrWhiteSpace(path) &&
        path.StartsWith('/') &&
        !path.StartsWith("//", StringComparison.Ordinal) &&
        !path.Contains('?') &&
        !path.Contains('#');

    private static void AddRequiredFailure(string value, string settingName, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"Authentication:OpenIdConnect:{settingName} is required when Authentication:Provider is OpenIdConnect.");
        }
    }
}
