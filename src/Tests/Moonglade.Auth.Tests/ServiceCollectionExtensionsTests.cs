using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Moonglade.Auth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("Local")]
    [InlineData("EntraID")]
    public async Task AddBlogAuthenticaton_RegistersLocalAccountTemporarySchemes(string provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Authentication:Provider"] = provider,
                ["Authentication:EntraID:Instance"] = "https://login.microsoftonline.com/",
                ["Authentication:EntraID:Domain"] = "example.com",
                ["Authentication:EntraID:TenantId"] = "00000000-0000-0000-0000-000000000000",
                ["Authentication:EntraID:ClientId"] = "00000000-0000-0000-0000-000000000001",
                ["Authentication:EntraID:CallbackPath"] = "/signin-oidc"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddBlogAuthenticaton(configuration);

        var providerServices = services.BuildServiceProvider();
        var schemeProvider = providerServices.GetRequiredService<IAuthenticationSchemeProvider>();
        var setupScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountSetup);
        var twoFactorScheme = await schemeProvider.GetSchemeAsync(BlogAuthSchemas.LocalAccountTwoFactor);

        Assert.NotNull(setupScheme);
        Assert.NotNull(twoFactorScheme);
    }
}
