using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moonglade.Auth.Oidc;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Moonglade.Auth.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddBlogAuthentication_TicketsAndBindingProofSurviveRestart()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var owner = new OwnerAccountSnapshot(Guid.NewGuid(), "owner@example.test", Guid.NewGuid(), true, true);
        var principal = LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local);
        var revision = Guid.NewGuid();
        var expires = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).AddMinutes(5);
        var schemes = new[]
        {
            BlogAuthSchemas.Local, BlogAuthSchemas.LocalAccountSetup, BlogAuthSchemas.LocalAccountTwoFactor,
            BlogAuthSchemas.InitialAccountSetup, OidcBindingFlow.PendingScheme
        };
        var tickets = new Dictionary<string, string>();
        string proof;

        await using (var first = CreateServices(connection, "first-content-root"))
        {
            await using var scope = first.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
            Assert.Empty(await db.DataProtectionKeys.AsNoTracking().ToListAsync(ct));

            var options = first.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            foreach (var scheme in schemes)
            {
                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = expires }, scheme);
                tickets.Add(scheme, options.Get(scheme).TicketDataFormat.Protect(ticket));
            }
            proof = first.GetRequiredService<OidcBindingFlow>().CreateProof(principal, revision)!;
            Assert.NotEmpty(await db.DataProtectionKeys.AsNoTracking().ToListAsync(ct));
        }

        await using var restarted = CreateServices(connection, "replacement-content-root");
        var restoredOptions = restarted.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        foreach (var scheme in schemes)
        {
            var ticket = restoredOptions.Get(scheme).TicketDataFormat.Unprotect(tickets[scheme]);
            Assert.NotNull(ticket);
            Assert.Equal(scheme, ticket.AuthenticationScheme);
            Assert.Equal(owner.Id.ToString(), ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal(owner.Revision.ToString(), ticket.Principal.FindFirstValue(LocalAccountPrincipal.RevisionClaim));
            Assert.Equal(expires, ticket.Properties.ExpiresUtc);
        }
        var flow = restarted.GetRequiredService<OidcBindingFlow>();
        Assert.True(flow.Matches(principal, revision, proof));
        Assert.False(flow.Matches(LocalAccountPrincipal.Create(owner, BlogAuthSchemas.Local), revision, proof));
    }

    [Fact]
    public async Task AddBlogAuthentication_PreservesOidcSecretProtectionAndIsolation()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var legacyServices = new ServiceCollection();
        legacyServices.AddLogging();
        legacyServices.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection));
        legacyServices.AddDataProtection()
            .SetApplicationName("Moonglade.OidcCredentials")
            .PersistKeysToDbContext<BlogDbContext>();
        string previousSecret;
        await using (var legacy = legacyServices.BuildServiceProvider(validateScopes: true))
        {
            await using var scope = legacy.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<BlogDbContext>().Database.EnsureCreatedAsync(ct);
            previousSecret = legacy.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("OidcClientSecret.v1").Protect("previous-synthetic-secret");
        }

        string savedSecret;
        await using (var application = CreateServices(connection, "new-content-root"))
        {
            var protection = application.GetRequiredService<IDataProtectionProvider>().CreateProtector("OidcClientSecret.v1");
            protection.Protect("application-payload");
            Assert.Throws<CryptographicException>(() => protection.Unprotect(previousSecret));

            await using var scope = application.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
            var owner = new OwnerAccountEntity
            {
                Id = Guid.NewGuid(),
                Email = "owner@example.test",
                NormalizedEmail = "OWNER@EXAMPLE.TEST",
                Revision = Guid.NewGuid()
            };
            db.OwnerAccount.Add(owner);
            await db.SaveChangesAsync(ct);
            await scope.ServiceProvider.GetRequiredService<OwnerOidcStore>().SaveAsync(owner.Id, null,
                "Synthetic provider", "https://identity.example.test", "synthetic-client", "replacement-synthetic-secret", ct);
            savedSecret = (await db.OwnerOidc.AsNoTracking().SingleAsync(ct)).ProtectedSecret;
            Assert.Throws<CryptographicException>(() => protection.Unprotect(savedSecret));
        }

        await using var reader = legacyServices.BuildServiceProvider(validateScopes: true);
        var credentials = reader.GetRequiredService<IDataProtectionProvider>().CreateProtector("OidcClientSecret.v1");
        Assert.Equal("previous-synthetic-secret", credentials.Unprotect(previousSecret));
        Assert.Equal("replacement-synthetic-secret", credentials.Unprotect(savedSecret));
    }

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

    private static ServiceProvider CreateServices(SqliteConnection connection, string hostDiscriminator)
    {
        var elements = new List<XElement>();
        var fallback = new Mock<IXmlRepository>();
        fallback.Setup(repository => repository.GetAllElements()).Returns(() => elements.ToArray());
        fallback.Setup(repository => repository.StoreElement(It.IsAny<XElement>(), It.IsAny<string>()))
            .Callback<XElement, string>((element, _) => elements.Add(new XElement(element)));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<BlogDbContext>(options => options.UseSqlite(connection));
        services.AddDataProtection().SetApplicationName(hostDiscriminator);
        services.Configure<KeyManagementOptions>(options => options.XmlRepository = fallback.Object);
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider(validateScopes: true);
    }
}
