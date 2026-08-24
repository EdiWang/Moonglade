using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;

namespace Moonglade.Auth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Local")]
    [InlineData("OpenIdConnect")]
    public async Task AddBlogAuthentication_RegistersLocalAccountTemporarySchemes(string provider)
    {
        var services = new ServiceCollection();
        services.AddBlogAuthentication(CreateConfiguration(provider));

        var providerServices = services.BuildServiceProvider();
        var schemeProvider = providerServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var setupScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountSetup);
        var twoFactorScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountTwoFactor);

        Assert.NotNull(setupScheme);
        Assert.NotNull(twoFactorScheme);
    }

    [Fact]
    public void AddBlogAuthentication_ConfiguresGenericOpenIdConnectHandler()
    {
        var services = new ServiceCollection();
        services.AddBlogAuthentication(CreateConfiguration("OpenIdConnect"));

        var providerServices = services.BuildServiceProvider();
        var options = providerServices
            .GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
            .Get(BlogAuthSchemas.OpenIdConnect);

        Assert.Equal("https://identity.example.com/", options.Authority);
        Assert.Equal("moonglade", options.ClientId);
        Assert.Equal("test-client-secret", options.ClientSecret);
        Assert.Equal("/signin-oidc", options.CallbackPath);
        Assert.Equal("/signout-callback-oidc", options.SignedOutCallbackPath);
        Assert.Equal(OpenIdConnectResponseType.Code, options.ResponseType);
        Assert.True(options.UsePkce);
        Assert.True(options.RequireHttpsMetadata);
        Assert.True(options.GetClaimsFromUserInfoEndpoint);
        Assert.False(options.SaveTokens);
        Assert.False(options.MapInboundClaims);
        Assert.Equal("preferred_username", options.TokenValidationParameters.NameClaimType);
        Assert.Equal(["openid", "profile", "email"], options.Scope);
    }

    [Theory]
    [InlineData("allowed-subject", true)]
    [InlineData("other-subject", false)]
    public async Task AdministratorPolicy_ForOpenIdConnect_RequiresAllowedSubject(string subject, bool expected)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlogAuthentication(CreateConfiguration("OpenIdConnect"));

        var providerServices = services.BuildServiceProvider();
        var authorizationService = providerServices.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", subject)],
            BlogAuthSchemas.OpenIdConnect));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            null,
            BlogAuthSchemas.AdministratorPolicy);

        Assert.Equal(expected, result.Succeeded);
    }

    [Fact]
    public async Task AdministratorPolicy_ForOpenIdConnectWithEmptyAllowlist_DeniesAccess()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlogAuthentication(CreateConfiguration("OpenIdConnect", includeAllowedSubject: false));

        var providerServices = services.BuildServiceProvider();
        var authorizationService = providerServices.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "any-subject")],
            BlogAuthSchemas.OpenIdConnect));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            null,
            BlogAuthSchemas.AdministratorPolicy);

        Assert.False(result.Succeeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdministratorPolicy_ForLocalAccount_RequiresAdministratorRole(bool includeAdministratorRole)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlogAuthentication(CreateConfiguration("Local"));

        var claims = new List<Claim> { new(ClaimTypes.Name, "admin") };
        if (includeAdministratorRole)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        }

        var providerServices = services.BuildServiceProvider();
        var authorizationService = providerServices.GetRequiredService<IAuthorizationService>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, BlogAuthSchemas.Local));

        var result = await authorizationService.AuthorizeAsync(
            principal,
            null,
            BlogAuthSchemas.AdministratorPolicy);

        Assert.Equal(includeAdministratorRole, result.Succeeded);
    }

    private static IConfiguration CreateConfiguration(string provider, bool includeAllowedSubject = true)
    {
        var values = new Dictionary<string, string>
        {
            ["Authentication:Provider"] = provider,
            ["Authentication:OpenIdConnect:Authority"] = "https://identity.example.com/",
            ["Authentication:OpenIdConnect:ClientId"] = "moonglade",
            ["Authentication:OpenIdConnect:ClientSecret"] = "test-client-secret",
            ["Authentication:OpenIdConnect:CallbackPath"] = "/signin-oidc",
            ["Authentication:OpenIdConnect:SignedOutCallbackPath"] = "/signout-callback-oidc",
            ["Authentication:OpenIdConnect:NameClaimType"] = "preferred_username",
            ["Authentication:OpenIdConnect:Scopes:0"] = "openid",
            ["Authentication:OpenIdConnect:Scopes:1"] = "profile",
            ["Authentication:OpenIdConnect:Scopes:2"] = "email"
        };

        if (includeAllowedSubject)
        {
            values["Authentication:OpenIdConnect:AllowedSubjects:0"] = "allowed-subject";
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
