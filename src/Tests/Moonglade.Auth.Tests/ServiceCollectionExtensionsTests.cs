using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Moonglade.Auth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddBlogAuthentication_RegistersLocalAccountTemporarySchemes()
    {
        var services = new ServiceCollection();
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());

        var providerServices = services.BuildServiceProvider();
        var schemeProvider = providerServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var setupScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountSetup);
        var twoFactorScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountTwoFactor);

        Assert.NotNull(setupScheme);
        Assert.NotNull(twoFactorScheme);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AdministratorPolicy_ForLocalAccount_RequiresAdministratorRole(bool includeAdministratorRole)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());

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
}
