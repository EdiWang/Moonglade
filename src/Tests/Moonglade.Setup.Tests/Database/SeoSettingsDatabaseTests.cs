using Microsoft.EntityFrameworkCore;
using Moonglade.Configuration;
using Moonglade.Data.Entities;

namespace Moonglade.Setup.Tests.Database;

public class SeoSettingsDatabaseTests(DisposableCategoryDatabases databases) : IClassFixture<DisposableCategoryDatabases>
{
    [DisposableDatabaseTheory]
    [InlineData("PostgreSql")]
    [InlineData("SqlServer")]
    public async Task Upgrade_CopiesLegacySeoValues_AndRerunPreservesEdits(string provider)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var database = await databases.CreateAsync(provider);
        await using var db = database.Context();
        await db.Database.EnsureCreatedAsync(ct);

        var originalTime = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        const string generalJson = """{"CANONICALprefix":"https://blog.example.test","metaKeyword":"a \"quote\"","useDublinCoreMetaData":true,"dcLicenseUrl":null,"siteTitle":"Preserved"}""";
        const string advancedJson = """{"EnableSiteMap":false,"enableJsonLdBlogPosting":false,"enableFoaf":true}""";
        db.BlogConfiguration.AddRange(
            new BlogConfigurationEntity { CfgKey = nameof(GeneralSettings), CfgValue = generalJson, LastModifiedTimeUtc = originalTime },
            new BlogConfigurationEntity { CfgKey = nameof(AdvancedSettings), CfgValue = advancedJson, LastModifiedTimeUtc = originalTime });
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();

        await database.MigrateAsync("seo-settings.sql");
        var rows = await db.BlogConfiguration.ToDictionaryAsync(row => row.CfgKey, ct);
        Assert.Equal(3, rows.Count);
        Assert.Equal(generalJson, rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(advancedJson, rows[nameof(AdvancedSettings)].CfgValue);
        Assert.Equal(originalTime, rows[nameof(GeneralSettings)].LastModifiedTimeUtc);
        Assert.Equal(originalTime, rows[nameof(AdvancedSettings)].LastModifiedTimeUtc);

        var (snapshot, defaults) = BlogConfig.Parse(rows.ToDictionary(pair => pair.Key, pair => pair.Value.CfgValue));
        Assert.DoesNotContain(nameof(SEOSettings), defaults.Keys);
        Assert.Equal(SEOSettings.DefaultValue with
        {
            CanonicalPrefix = "https://blog.example.test",
            MetaKeyword = "a \"quote\"",
            UseDublinCoreMetaData = true,
            EnableSiteMap = false,
            EnableJsonLdBlogPosting = false
        }, snapshot.SEOSettings);

        var migrated = rows[nameof(SEOSettings)];
        var edited = snapshot.SEOSettings with { MetaKeyword = "Edited" };
        migrated.CfgValue = edited.ToJson();
        migrated.LastModifiedTimeUtc = originalTime.AddDays(1);
        await db.SaveChangesAsync(ct);

        await database.MigrateAsync("seo-settings.sql");
        var retained = await db.BlogConfiguration.AsNoTracking().SingleAsync(row => row.CfgKey == nameof(SEOSettings), ct);
        Assert.Equal(edited.ToJson(), retained.CfgValue);
        Assert.Equal(migrated.LastModifiedTimeUtc, retained.LastModifiedTimeUtc);
    }
}
