using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Auth;
using Moonglade.Auth.Oidc;
using Moonglade.Data;

namespace Moonglade.Setup.Tests.Database;

public partial class OwnerAccountDatabaseTests
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task OidcSchema_UpgradesAndReruns_AndCopiedDatabaseStateRecoversSecret(string provider)
    {
        const string syntheticSecret = "oidc-test-only-value";
        const string authority = "https://issuer.example.test";
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);
        var accounts = CreateStore(db);
        var setup = await accounts.PrepareLocalSetupAsync("local@example.test", "Synthetic123!", ct);
        var owner = await accounts.CompleteLocalSetupAsync(setup, null, ct);
        var ownerRow = await db.OwnerAccount.AsNoTracking().SingleAsync(ct);
        await DropOidcSchemaAsync(database);
        await database.MigrateAsync("owner-oidc.sql");

        await using (var services = CreateOidcServices(database))
        {
            await using var scope = services.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<OwnerOidcStore>();
            await store.SaveAsync(owner.Id, null, "Test provider", authority, "test-client", syntheticSecret, ct);
            Assert.Equal(syntheticSecret, (await store.ReadBindingCredentialsAsync(ct))!.ClientSecret);
        }

        var row = await db.OwnerOidc.AsNoTracking().SingleAsync(ct);
        var keys = await db.DataProtectionKeys.AsNoTracking().ToListAsync(ct);
        Assert.NotEmpty(keys);
        Assert.NotEqual(syntheticSecret, row.ProtectedSecret);
        await database.MigrateAsync("owner-oidc.sql");
        Assert.Equal(row.ProtectedSecret, (await db.OwnerOidc.AsNoTracking().SingleAsync(ct)).ProtectedSecret);
        Assert.Equal(keys.Select(key => key.Id), await db.DataProtectionKeys.OrderBy(key => key.Id).Select(key => key.Id).ToListAsync(ct));
        Assert.Equal(owner, await accounts.ReadAsync(ct));

        await using var replacement = await databases.CreateAsync(provider);
        await using var copied = replacement.Context();
        await copied.Database.EnsureCreatedAsync(ct);
        copied.OwnerAccount.Add(ownerRow);
        copied.OwnerOidc.Add(row);
        copied.DataProtectionKeys.AddRange(keys.Select(key => new DataProtectionKey { FriendlyName = key.FriendlyName, Xml = key.Xml }));
        await copied.SaveChangesAsync(ct);
        await using var replacementServices = CreateOidcServices(replacement);
        await using var replacementScope = replacementServices.CreateAsyncScope();
        var replacementStore = replacementScope.ServiceProvider.GetRequiredService<OwnerOidcStore>();
        Assert.Equal(syntheticSecret, (await replacementStore.ReadBindingCredentialsAsync(ct))!.ClientSecret);
        await replacementStore.BindAsync(owner.Id, row.Revision, authority, "test-subject", "External account", ct);
        await replacement.MigrateAsync("owner-oidc.sql");
        Assert.Equal("test-subject", (await copied.OwnerOidc.AsNoTracking().SingleAsync(ct)).Subject);
    }

    private static ServiceProvider CreateOidcServices(CategoryTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<BlogDbContext>(_ => database.Context());
        services.AddBlogAuthentication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static Task DropOidcSchemaAsync(CategoryTestDatabase database) =>
        database.ExecuteAsync(database.Provider == "PostgreSql"
            ? "DROP TABLE public.owner_oidc; DROP TABLE public.data_protection_keys;"
            : "DROP TABLE dbo.OwnerOidc; DROP TABLE dbo.DataProtectionKeys;");
}
