using Microsoft.Extensions.Options;

namespace Moonglade.Auth.Tests;

public class AuthenticationSettingsValidatorTests
{
    private readonly AuthenticationSettingsValidator _sut = new();

    [Fact]
    public void Validate_LocalAccountWithoutOpenIdConnectSettings_Succeeds()
    {
        var result = _sut.Validate(Options.DefaultName, new AuthenticationSettings
        {
            Provider = AuthenticationProvider.Local
        });

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_OpenIdConnectWithRequiredSettings_Succeeds()
    {
        var result = _sut.Validate(Options.DefaultName, CreateValidOpenIdConnectSettings());

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_OpenIdConnectWithMissingRequiredSettings_Fails()
    {
        var result = _sut.Validate(Options.DefaultName, new AuthenticationSettings
        {
            Provider = AuthenticationProvider.OpenIdConnect,
            OpenIdConnect = new OpenIdConnectAuthenticationSettings
            {
                Authority = "http://identity.example.com/",
                Scopes = ["profile"],
                AllowedSubjects = [" "]
            }
        });

        Assert.True(result.Failed);
        Assert.Contains("Authority", result.FailureMessage);
        Assert.Contains("ClientId", result.FailureMessage);
        Assert.Contains("ClientSecret", result.FailureMessage);
        Assert.Contains("Scopes", result.FailureMessage);
        Assert.Contains("AllowedSubjects", result.FailureMessage);
    }

    [Fact]
    public void Validate_OpenIdConnectWithEmptyAllowedSubjects_SucceedsForBootstrap()
    {
        var options = CreateValidOpenIdConnectSettings();
        options.OpenIdConnect.AllowedSubjects = [];

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validate_OpenIdConnectWithMissingSection_Fails()
    {
        var result = _sut.Validate(Options.DefaultName, new AuthenticationSettings
        {
            Provider = AuthenticationProvider.OpenIdConnect,
            OpenIdConnect = null!
        });

        Assert.True(result.Failed);
        Assert.Contains("Authentication:OpenIdConnect is required", result.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("signin-oidc")]
    [InlineData("//signin-oidc")]
    [InlineData("/signin-oidc?returnUrl=/")]
    public void Validate_OpenIdConnectWithInvalidCallbackPath_Fails(string callbackPath)
    {
        var options = CreateValidOpenIdConnectSettings();
        options.OpenIdConnect.CallbackPath = callbackPath;

        var result = _sut.Validate(Options.DefaultName, options);

        Assert.True(result.Failed);
        Assert.Contains("CallbackPath", result.FailureMessage);
    }

    private static AuthenticationSettings CreateValidOpenIdConnectSettings() =>
        new()
        {
            Provider = AuthenticationProvider.OpenIdConnect,
            OpenIdConnect = new OpenIdConnectAuthenticationSettings
            {
                Authority = "https://identity.example.com/",
                ClientId = "moonglade",
                ClientSecret = "test-client-secret",
                AllowedSubjects = ["allowed-subject"]
            }
        };
}
