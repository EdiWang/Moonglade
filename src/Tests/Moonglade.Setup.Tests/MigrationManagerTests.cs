using Edi.AspNetCore.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Setup.Tests;

public sealed class MigrationManagerTests
{
    [Fact]
    public async Task TryMigrationAsync_NullContext_ThrowsArgumentNullException()
    {
        var manager = CreateManager();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.TryMigrationAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task TryMigrationAsync_CurrentManifest_DoesNotRequireAutomaticMigration()
    {
        await using var context = CreateContext(SystemManifestSettings.DefaultValueNew.ToJson());
        var manager = CreateManager(autoMigrationEnabled: false);

        var result = await manager.TryMigrationAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(MigrationStatus.NotRequired, result.Status);
        Assert.True(result.CanContinueStartup);
    }

    [Fact]
    public async Task TryMigrationAsync_InvalidManifest_ReturnsVersionParsingError()
    {
        await using var context = CreateContext("not-json");
        var manager = CreateManager();

        var result = await manager.TryMigrationAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(MigrationStatus.VersionParsingError, result.Status);
        Assert.False(result.CanContinueStartup);
    }

    [Fact]
    public async Task TryMigrationAsync_OlderManifestOnPrereleaseBuild_StopsStartup()
    {
        if (!VersionHelper.IsNonStableVersion())
        {
            return;
        }

        var manifest = new SystemManifestSettings
        {
            VersionString = "1.0.0",
            InstallTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await using var context = CreateContext(manifest.ToJson());
        var manager = CreateManager();

        var result = await manager.TryMigrationAsync(context, TestContext.Current.CancellationToken);

        Assert.Equal(MigrationStatus.UnsupportedVersion, result.Status);
        Assert.False(result.CanContinueStartup);
    }

    [Theory]
    [InlineData(MigrationStatus.Success, true)]
    [InlineData(MigrationStatus.NotRequired, true)]
    [InlineData(MigrationStatus.ManualMigrationRequired, false)]
    [InlineData(MigrationStatus.UnsupportedVersion, false)]
    [InlineData(MigrationStatus.VersionParsingError, false)]
    [InlineData(MigrationStatus.UnsupportedProvider, false)]
    [InlineData(MigrationStatus.Failed, false)]
    public void MigrationResult_CanContinueStartup_MatchesStatus(MigrationStatus status, bool expected)
    {
        var result = new MigrationResult(status);

        Assert.Equal(expected, result.CanContinueStartup);
    }

    private static MigrationManager CreateManager(bool autoMigrationEnabled = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AutoDatabaseMigration"] = autoMigrationEnabled.ToString()
            })
            .Build();

        return new MigrationManager(NullLogger<MigrationManager>.Instance, configuration);
    }

    private static BlogDbContext CreateContext(string manifestJson)
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new BlogDbContext(options);
        context.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = nameof(SystemManifestSettings),
            CfgValue = manifestJson,
            LastModifiedTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        context.SaveChanges();
        return context;
    }
}
