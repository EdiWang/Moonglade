using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Setup.Tests.TestSupport;

namespace Moonglade.Setup.Tests;

[Collection("Startup site icon cache")]
public class StartUpInitializerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("16.7.0")]
    public async Task InitStartUpAsync_ExistingDatabase_PreservesContentAndManifest(string manifestVersion)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var originalTime = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.AddRange(
            new BlogConfigurationEntity { CfgKey = nameof(ContentSettings), CfgValue = "{}", LastModifiedTimeUtc = originalTime },
            new BlogConfigurationEntity { CfgKey = "LocalAccountSettings", CfgValue = "inert-legacy-json", LastModifiedTimeUtc = originalTime });
        var manifest = new SystemManifestSettings
        {
            VersionString = manifestVersion,
            InstallTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        if (manifestVersion is not null)
            db.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = nameof(SystemManifestSettings),
                CfgValue = manifest.ToJson(),
                LastModifiedTimeUtc = originalTime
            });
        var post = new PostEntity { Id = Guid.NewGuid(), Title = "Historical post", ContentType = "", CreateTimeUtc = originalTime };
        db.Post.Add(post);
        await db.SaveChangesAsync(ct);
        using var harness = new StartupTestHarness(db);
        var before = DateTime.UtcNow;

        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));

        db.ChangeTracker.Clear();
        var savedPost = await db.Post.SingleAsync(ct);
        Assert.Equal(post.Id, savedPost.Id);
        Assert.Equal("Historical post", savedPost.Title);
        Assert.Empty(savedPost.ContentType);
        Assert.Equal(originalTime, savedPost.CreateTimeUtc);
        var settings = await db.BlogConfiguration.OrderBy(row => row.CfgKey).ToListAsync(ct);
        Assert.Equal(12, settings.Count);
        Assert.Equal(originalTime, settings.Single(row => row.CfgKey == nameof(ContentSettings)).LastModifiedTimeUtc);
        Assert.Equal(originalTime, settings.Single(row => row.CfgKey == "LocalAccountSettings").LastModifiedTimeUtc);
        Assert.Equal("inert-legacy-json", settings.Single(row => row.CfgKey == "LocalAccountSettings").CfgValue);
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        if (manifestVersion is not null)
        {
            Assert.Equal(manifest.ToJson(), harness.Manifest.ToJson());
            Assert.DoesNotContain(harness.ConfigLogger.Entries, entry => entry.Level == LogLevel.Warning);
        }
        else
        {
            Assert.Equal("0.0.0.0", harness.Manifest.VersionString);
            Assert.InRange(harness.Manifest.InstallTimeUtc, before, DateTime.UtcNow);
            Assert.Contains(harness.ConfigLogger.Entries, entry => entry.Level == LogLevel.Warning
                && entry.Message.Contains(nameof(SystemManifestSettings)) && entry.Message.Contains("reconstruction"));
        }
        var snapshots = settings.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)).ToArray();
        Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
        db.ChangeTracker.Clear();
        var after = await db.BlogConfiguration.OrderBy(row => row.CfgKey).ToListAsync(ct);
        Assert.Equal(snapshots, after.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)).ToArray());
        Assert.Single(await db.Post.ToListAsync(ct));
    }

    public static IEnumerable<object[]> ApplicationEntityTypes => typeof(BlogDbContext).GetProperties()
        .Where(property => property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .Select(property => new object[] { property.PropertyType.GetGenericArguments()[0] });

    [Theory]
    [MemberData(nameof(ApplicationEntityTypes))]
    public async Task InitStartUpAsync_AnyExistingApplicationRowWithoutConfiguration_IsPreserved(Type entityType)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var entity = Activator.CreateInstance(entityType)!;
        foreach (var property in entityType.GetProperties().Where(property => property.CanWrite))
        {
            if (property.PropertyType == typeof(string)) property.SetValue(entity, "Preserved");
            if (property.PropertyType == typeof(Guid)) property.SetValue(entity, Guid.NewGuid());
            if (property.PropertyType == typeof(int)) property.SetValue(entity, 100);
            if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                property.SetValue(entity, DateTime.UtcNow.AddDays(-1));
        }
        if (entity is OwnerAvatarEntity)
            entity = new OwnerAvatarEntity { UseGravatar = true };
        if (entity is ActivityLogEntity activity)
        {
            activity.EventKey = "post.created";
            activity.ActorKind = ActivityActorKind.Administrator;
            activity.ActorDisplayName = "Preserved administrator";
            activity.Details = "{}";
        }
        db.Add(entity);
        await db.SaveChangesAsync(ct);
        var entry = db.Entry(entity);
        var keys = entry.Metadata.FindPrimaryKey()!.Properties.Select(property => entry.Property(property.Name).CurrentValue).ToArray();
        var snapshot = entry.Properties.ToDictionary(property => property.Metadata.Name, property => property.CurrentValue);
        db.ChangeTracker.Clear();
        using var harness = new StartupTestHarness(db);
        var originalRuntime = harness.Config.Capture().GeneralSettings;

        Assert.Equal(InitStartUpResult.FailedInitBlogConfig, await harness.Initializer.InitStartUpAsync(ct));

        db.ChangeTracker.Clear();
        var retained = await db.FindAsync(entityType, keys, ct);
        Assert.NotNull(retained);
        Assert.Equal(snapshot, db.Entry(retained).Properties.ToDictionary(property => property.Metadata.Name, property => property.CurrentValue));
        Assert.Same(originalRuntime, harness.Config.Capture().GeneralSettings);
        Assert.False(await db.BlogConfiguration.AnyAsync(row => row.CfgKey == "LocalAccountSettings", ct));
        Assert.Contains(harness.ConfigLogger.Entries, log => log.Level == LogLevel.Error);
    }

    [Theory]
    [InlineData(nameof(SystemManifestSettings))]
    public async Task InitStartUpAsync_InvalidManifest_PreventsAllRepairsAndPublication(string key)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        db.BlogConfiguration.AddRange(
            new BlogConfigurationEntity { CfgKey = "LocalAccountSettings", CfgValue = "inert-legacy-json" },
            new BlogConfigurationEntity { CfgKey = nameof(SystemManifestSettings), CfgValue = SystemManifestSettings.DefaultValue.ToJson() },
            new BlogConfigurationEntity { CfgKey = nameof(FeedSettings), CfgValue = "not-json" });
        db.BlogConfiguration.Local.Single(row => row.CfgKey == key).CfgValue = "null";
        await db.SaveChangesAsync(ct);
        using var harness = new StartupTestHarness(db);
        var original = harness.Config.Capture().GeneralSettings;

        Assert.Equal(InitStartUpResult.FailedInitBlogConfig, await harness.Initializer.InitStartUpAsync(ct));

        Assert.Same(original, harness.Config.Capture().GeneralSettings);
        db.ChangeTracker.Clear();
        Assert.Equal(3, await db.BlogConfiguration.CountAsync(ct));
        Assert.Equal("not-json", (await db.BlogConfiguration.SingleAsync(row => row.CfgKey == nameof(FeedSettings), ct)).CfgValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InitStartUpAsync_AccountAbsentOrProfileOnly_PreservesExistingInstallationAcrossRestart(bool profileOnly)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var timestamp = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var settings = StartupSettings.Defaults();
        db.BlogConfiguration.AddRange(settings.Select(pair => new BlogConfigurationEntity
        {
            CfgKey = pair.Key,
            CfgValue = pair.Value,
            LastModifiedTimeUtc = timestamp
        }));
        var post = new PostEntity { Id = Guid.NewGuid(), Title = "Existing title", PostContent = "Existing content", CreateTimeUtc = timestamp };
        db.Post.Add(post);
        db.Comment.Add(new CommentEntity
        {
            Id = Guid.NewGuid(),
            PostId = post.Id,
            Username = "Visitor",
            Email = "visitor@example.test",
            CommentContent = "Existing comment",
            CreateTimeUtc = timestamp
        });
        var profileId = Guid.NewGuid();
        if (profileOnly)
            db.OwnerAccount.Add(new OwnerAccountEntity
            {
                Id = profileId,
                Email = "profile@example.test",
                NormalizedEmail = "PROFILE@EXAMPLE.TEST",
                Revision = Guid.NewGuid()
            });
        await db.SaveChangesAsync(ct);

        for (var restart = 0; restart < 2; restart++)
        {
            using var harness = new StartupTestHarness(db);
            Assert.Equal(InitStartUpResult.Success, await harness.Initializer.InitStartUpAsync(ct));
            db.ChangeTracker.Clear();
            var saved = await db.Post.SingleAsync(ct);
            Assert.Equal(post.Id, saved.Id);
            Assert.Equal("Existing content", saved.PostContent);
            Assert.Equal("Existing comment", (await db.Comment.SingleAsync(ct)).CommentContent);
            Assert.Empty(await db.Category.ToListAsync(ct));
            Assert.Empty(await db.Widget.ToListAsync(ct));
            foreach (var row in await db.BlogConfiguration.ToListAsync(ct))
            {
                Assert.Equal(settings[row.CfgKey], row.CfgValue);
                Assert.Equal(timestamp, row.LastModifiedTimeUtc);
            }
            Assert.Equal(settings.Count, await db.BlogConfiguration.CountAsync(ct));
            var owner = await db.OwnerAccount.SingleOrDefaultAsync(ct);
            if (profileOnly)
            {
                Assert.Equal(profileId, owner!.Id);
                Assert.Equal("profile@example.test", owner.Email);
                Assert.Null(owner.LocalInitializedUtc);
                Assert.Null(owner.PasswordHash);
                Assert.Null(owner.PasswordSalt);
                Assert.Null(owner.TotpSecret);
            }
            else Assert.Null(owner);
        }
    }

    private static BlogDbContext CreateContext() => new(new DbContextOptionsBuilder<BlogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

[CollectionDefinition("Startup site icon cache", DisableParallelization = true)]
public class StartupSiteIconCacheCollection;
