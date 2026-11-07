using LiteBus.Events.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Web.Tests.Structure;

public class CommitBoundaryProbeTests
{
    [Fact]
    public async Task ExistingCommand_OuterTransactionPublishesBeforeOuterCommit_AndRollbackCannotUndoRuntime()
    {
        // Characterize the unsupported composition so S4 cannot mistake it for a safe commit boundary.
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = Context(connection);
        await Seed(db);
        var config = new BlogConfig(new BlogSettingsSnapshot { FeedSettings = FeedSettings.DefaultValue });
        var events = new Mock<IEventMediator>();
        await using var outer = await db.Database.BeginTransactionAsync(ct);

        await Handler(db, config, events.Object).HandleAsync(UpdateConfigurationCommand.Create(new FeedSettings { FeedItemCount = 1 }), ct);

        Assert.Same(outer, db.Database.CurrentTransaction);
        Assert.Single(events.Invocations);
        Assert.Equal(1, config.Capture().FeedSettings.FeedItemCount);
        await outer.RollbackAsync(ct);
        db.ChangeTracker.Clear();
        var restored = (await db.BlogConfiguration.SingleAsync(ct)).CfgValue.FromJson<FeedSettings>();
        Assert.Equal(20, restored.FeedItemCount);
        Assert.Equal(1, config.Capture().FeedSettings.FeedItemCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StagedAssetAndSettings_OneSaveCommitsTogether_AndOnlyThenPublishes(bool failSave)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = Context(connection);
        await Seed(db);
        if (failSave)
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_settings BEFORE UPDATE ON BlogConfiguration BEGIN SELECT RAISE(ABORT, 'Settings rejected'); END;", ct);
        var config = new BlogConfig(new BlogSettingsSnapshot { FeedSettings = FeedSettings.DefaultValue });
        var original = config.Capture().FeedSettings;
        var assetId = Guid.NewGuid();
        db.BlogAsset.Add(new BlogAssetEntity { Id = assetId, Base64Data = "AQID", LastModifiedTimeUtc = DateTime.UtcNow });
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback(() =>
            {
                Assert.Null(db.Database.CurrentTransaction);
                using var verification = Context(connection);
                Assert.Equal("AQID", verification.BlogAsset.AsNoTracking().Single().Base64Data);
                Assert.Equal(1, verification.BlogConfiguration.AsNoTracking().Single().CfgValue.FromJson<FeedSettings>().FeedItemCount);
                Assert.Equal(1, config.Capture().FeedSettings.FeedItemCount);
            }).Returns(Task.CompletedTask);
        var command = UpdateConfigurationCommand.Create(new FeedSettings { FeedItemCount = 1 });

        if (failSave) await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db, config, events.Object).HandleAsync(command, ct));
        else await Handler(db, config, events.Object).HandleAsync(command, ct);

        db.ChangeTracker.Clear();
        Assert.Equal(!failSave, await db.BlogAsset.AnyAsync(row => row.Id == assetId, ct));
        Assert.Equal(failSave ? 20 : 1, (await db.BlogConfiguration.SingleAsync(ct)).CfgValue.FromJson<FeedSettings>().FeedItemCount);
        if (failSave)
        {
            Assert.Same(original, config.Capture().FeedSettings);
            Assert.Empty(events.Invocations);
        }
        else Assert.Single(events.Invocations);
    }

    private static BlogDbContext Context(SqliteConnection connection) => new(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(connection).Options);

    private static UpdateConfigurationCommandHandler Handler(BlogDbContext db, BlogConfig config, IEventMediator events) =>
        new(db, config, events, new ConfigurationWriteGate(), NullLogger<UpdateConfigurationCommandHandler>.Instance);

    private static async Task Seed(BlogDbContext db)
    {
        var ct = TestContext.Current.CancellationToken;
        await db.Database.EnsureCreatedAsync(ct);
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = nameof(FeedSettings),
            CfgValue = FeedSettings.DefaultValue.ToJson(),
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(ct);
    }
}
