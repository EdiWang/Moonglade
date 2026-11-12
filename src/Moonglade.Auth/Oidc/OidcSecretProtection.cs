using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Auth.Oidc;

internal sealed class OidcSecretProtection : IDisposable
{
    private readonly ServiceProvider _services;
    public IDataProtector Protector { get; }

    public OidcSecretProtection(IServiceProvider applicationServices, ILoggerFactory loggerFactory)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("Moonglade.OidcCredentials");
        services.Configure<KeyManagementOptions>(options =>
            options.XmlRepository = new EntityFrameworkCoreXmlRepository<BlogDbContext>(applicationServices, loggerFactory));
        _services = services.BuildServiceProvider();
        Protector = _services.GetRequiredService<IDataProtectionProvider>().CreateProtector("OidcClientSecret.v1");
    }

    public void Dispose() => _services.Dispose();
}
