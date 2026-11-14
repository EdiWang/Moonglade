using LiteBus.Events.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Moonglade.Configuration.Tests;

public partial class UpdateConfigurationCommandTests
{
    private readonly ConfigurationWriteGate _gate = new();
    [Fact]
    public async Task Save_UpdatesDatabaseAndRuntimeFromRetainedImmutableValue()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        await SeedAsync(db, config.Capture().GeneralSettings);
        var original = config.Capture().GeneralSettings;
        var model = General("New");
        model = model with { Copyright = "Copyright" };
        var submitted = model;
        var command = UpdateConfigurationCommand.Create(model);
        model = model with { SiteTitle = "Changed after dispatch" };

        var result = await Handler(db, config).HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New"), Assert.Single(result.Changes));
        Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal("Old", original.SiteTitle);
        Assert.Same(submitted, command.Settings);
        Assert.Same(submitted, config.Capture().GeneralSettings);
        Assert.NotSame(model, config.Capture().GeneralSettings);
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
        model = model with { Copyright = "Changed after save" };
        Assert.Equal("Copyright", config.Capture().GeneralSettings.Copyright);
    }

    [Fact]
    public async Task UnchangedValue_PreservesTimestampAndRefreshesRuntime()
    {
        using var db = CreateDbContext();
        var settings = General("Persisted");
        await SeedAsync(db, settings);
        var originalTime = (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).LastModifiedTimeUtc;
        var config = new BlogConfig();

        await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(settings), TestContext.Current.CancellationToken);

        Assert.Equal(originalTime, (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).LastModifiedTimeUtc);
        Assert.Equal("Persisted", config.Capture().GeneralSettings.SiteTitle);
    }

    [Fact]
    public async Task MissingSetting_PreservesOtherRowsAndRuntime()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        await SeedAsync(db, config.Capture().GeneralSettings);
        var originalGeneral = config.Capture().GeneralSettings;
        var originalImage = config.Capture().ImageSettings;
        var command = UpdateConfigurationCommand.Create(CdnImage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(db, config)
            .HandleAsync(command, TestContext.Current.CancellationToken));

        Assert.Same(originalGeneral, config.Capture().GeneralSettings);
        Assert.Same(originalImage, config.Capture().ImageSettings);
        Assert.Equal(originalGeneral.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task SaveFailure_PreservesRuntimeAndPersistedSettings()
    {
        var interceptor = new FailingSaveInterceptor();
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        await SeedAsync(db, config.Capture().GeneralSettings);
        var original = config.Capture().GeneralSettings;
        interceptor.Fail = true;

        var updated = General("Old");
        updated = updated with { SiteTitle = "Changed" };
        await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(updated),
            TestContext.Current.CancellationToken));

        Assert.Same(original, config.Capture().GeneralSettings);
        db.ChangeTracker.Clear();
        Assert.Equal(original.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task CancelledSave_PreservesRuntimeAndAllowsLaterSave()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig();
        await SeedAsync(db, config.Capture().GeneralSettings);
        var original = config.Capture().GeneralSettings;
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var command = UpdateConfigurationCommand.Create(General("New"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Handler(db, config).HandleAsync(command, cancelled.Token));

        Assert.Same(original, config.Capture().GeneralSettings);
        await Handler(db, config).HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.Equal("New", config.Capture().GeneralSettings.SiteTitle);
    }

    [Fact]
    public async Task ConcurrentSaves_KeepRuntimeInDatabaseCommitOrder()
    {
        var interceptor = new BlockingSaveInterceptor();
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        using var firstDb = new BlogDbContext(options);
        using var secondDb = new BlogDbContext(options);
        var config = new BlogConfig();
        await SeedAsync(firstDb, config.Capture().GeneralSettings);
        interceptor.Enabled = true;
        var first = Handler(firstDb, config).HandleAsync(UpdateConfigurationCommand.Create(
            General("First")), TestContext.Current.CancellationToken);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = Handler(secondDb, config).HandleAsync(UpdateConfigurationCommand.Create(
            General("Second")), TestContext.Current.CancellationToken);
        var secondWasBlocked = !second.IsCompleted;
        interceptor.Resume.SetResult();

        await Task.WhenAll(first, second);

        Assert.Equal(new SettingValueChange("SiteTitle", null, "First"),
            Assert.Single(first.Result.Changes, change => change.Key == "SiteTitle"));
        Assert.Equal(new SettingValueChange("SiteTitle", "First", "Second"), Assert.Single(second.Result.Changes));
        Assert.True(secondWasBlocked);
        Assert.Equal("Second", config.Capture().GeneralSettings.SiteTitle);
        secondDb.ChangeTracker.Clear();
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), (await secondDb.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task QueuedPatchesToSameGroup_PreserveBothFieldChanges()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new BlockingSaveInterceptor();
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        using var firstDb = new BlogDbContext(options);
        using var secondDb = new BlogDbContext(options);
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        var original = config.Capture().GeneralSettings;
        await SeedAsync(firstDb, original);
        var title = Patch(nameof(GeneralSettings.SiteTitle), "New title");
        var logo = Patch(nameof(GeneralSettings.LogoText), "New logo");
        interceptor.Enabled = true;

        var firstSave = Handler(firstDb, config).HandleAsync(title, ct);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
        var secondSave = Handler(secondDb, config).HandleAsync(logo, ct);
        var secondWasBlocked = !secondSave.IsCompleted;
        interceptor.Resume.SetResult();
        await Task.WhenAll(firstSave, secondSave);

        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New title"), Assert.Single(firstSave.Result.Changes));
        Assert.Equal(new SettingValueChange("LogoText", "moonglade", "New logo"), Assert.Single(secondSave.Result.Changes));
        Assert.True(secondWasBlocked);
        var saved = config.Capture().GeneralSettings;
        Assert.Equal(original with { SiteTitle = "New title", LogoText = "New logo" }, saved);
        Assert.Equal(saved.ToJson(), (await secondDb.BlogConfiguration.AsNoTracking().SingleAsync(ct)).CfgValue);

        static PatchConfigurationCommand Patch(string name, string value) =>
            PatchConfigurationCommand.Create<GeneralSettings>(static current => current with { },
                JsonSerializer.SerializeToElement(new Dictionary<string, string> { [name] = value }), name);
    }

    [Fact]
    public async Task CancellationAfterCommit_StillAppliesPersistedValue()
    {
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancellingSaveInterceptor(cancellation);
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig();
        await SeedAsync(db, config.Capture().GeneralSettings);
        interceptor.Enabled = true;
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback<IEvent, EventMediationSettings, CancellationToken>((message, _, _) =>
            {
                Assert.True(cancellation.IsCancellationRequested);
                Assert.Equal("Committed", config.Capture().GeneralSettings.SiteTitle);
                Assert.Equal(config.Capture().GeneralSettings.ToJson(), db.BlogConfiguration.Local.Single().CfgValue);
                Assert.Equal([nameof(GeneralSettings)], ((ConfigurationChangedEvent)message).Names);
            }).Returns(Task.CompletedTask);

        var result = await new UpdateConfigurationCommandHandler(db, config, events.Object, _gate, NullLogger<UpdateConfigurationCommandHandler>.Instance)
            .HandleAsync(UpdateConfigurationCommand.Create(General("Committed")), cancellation.Token);

        Assert.Equal(new SettingValueChange("SiteTitle", null, "Committed"),
            Assert.Single(result.Changes, change => change.Key == "SiteTitle"));
        Assert.True(cancellation.IsCancellationRequested);
        events.Verify(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None), Times.Once);
        Assert.Equal("Committed", config.Capture().GeneralSettings.SiteTitle);
        Assert.Equal(config.Capture().GeneralSettings.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task UnknownSetting_IsRejectedBeforeDatabaseChanges()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig();
        await SeedAsync(db, config.Capture().GeneralSettings);
        var original = config.Capture().GeneralSettings;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(new UnregisteredSettings()), TestContext.Current.CancellationToken));

        Assert.Same(original, config.Capture().GeneralSettings);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [Fact]
    public void NullSettings_CannotCreateUpdateCommand() =>
        Assert.Throws<ArgumentNullException>(() => UpdateConfigurationCommand.Create<GeneralSettings>(null!));

    private static BlogDbContext CreateDbContext(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptors).Options);

    [Theory]
    [InlineData("invalid")]
    [InlineData("missing")]
    [InlineData("failed-save")]
    [InlineData("cancelled")]
    [InlineData("unchanged")]
    public async Task RejectedOrUnchangedWrite_DoesNotPublishChange(string scenario)
    {
        var interceptor = new FailingSaveInterceptor();
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old") });
        if (scenario != "missing") await SeedAsync(db, config.Capture().GeneralSettings);
        var originalSnapshot = config.Capture();
        interceptor.Fail = scenario == "failed-save";
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (scenario == "cancelled") await cancellation.CancelAsync();
        var events = new Mock<IEventMediator>();
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, _gate, NullLogger<UpdateConfigurationCommandHandler>.Instance);
        var settings = General(scenario == "unchanged" ? "Old" : "New");
        if (scenario == "invalid") settings = settings with { SiteTitle = null };

        var error = await Record.ExceptionAsync(() => handler.HandleAsync(UpdateConfigurationCommand.Create(settings), cancellation.Token));

        if (scenario == "unchanged")
        {
            Assert.Null(error);
            Assert.NotSame(originalSnapshot, config.Capture());
        }
        else
        {
            Assert.NotNull(error);
            Assert.Same(originalSnapshot, config.Capture());
        }
        Assert.Empty(events.Invocations);
    }

    [Fact]
    public async Task PublicationAndQueuedUnrelatedSave_RetainOldSnapshotsAndLatestGroups()
    {
        var ct = TestContext.Current.CancellationToken;
        var interceptor = new BlockingSaveInterceptor();
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor).Options;
        using var firstDb = new BlogDbContext(options);
        using var secondDb = new BlogDbContext(options);
        var config = new BlogConfig(new BlogSettingsSnapshot
        {
            GeneralSettings = General("Old"),
            ImageSettings = ImageSettings.DefaultValue,
            FeedSettings = FeedSettings.DefaultValue
        });
        await SeedAsync(firstDb, config.Capture().GeneralSettings);
        await SeedAsync(firstDb, config.Capture().ImageSettings);
        await SeedAsync(firstDb, config.Capture().FeedSettings);
        var retained = config.Capture();
        var queuedCommand = UpdateConfigurationCommand.Create(retained.FeedSettings with { FeedItemCount = 7 });
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback(() =>
            {
                var published = config.Capture();
                Assert.Equal("New", published.GeneralSettings.SiteTitle);
                Assert.Same(retained.ImageSettings, published.ImageSettings);
            }).Returns(Task.CompletedTask);
        interceptor.Enabled = true;
        var firstSave = new UpdateConfigurationCommandHandler(firstDb, config, events.Object, _gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance).HandleAsync(UpdateConfigurationCommand.Create(
            General("New")), ct);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);

        Assert.Same(retained, config.Capture());
        Assert.Same(retained.ImageSettings, config.Capture().ImageSettings);
        var secondSave = Handler(secondDb, config).HandleAsync(queuedCommand, ct);
        var secondWasBlocked = !secondSave.IsCompleted;
        interceptor.Resume.SetResult();
        await Task.WhenAll(firstSave, secondSave);

        Assert.Equal(new SettingValueChange("SiteTitle", "Old", "New"), Assert.Single(firstSave.Result.Changes));
        Assert.Equal(new SettingValueChange("FeedItemCount", 20, 7), Assert.Single(secondSave.Result.Changes));
        Assert.True(secondWasBlocked);
        Assert.Single(events.Invocations);
        var current = config.Capture();
        Assert.NotSame(retained, current);
        Assert.Equal("New", current.GeneralSettings.SiteTitle);
        Assert.Same(retained.ImageSettings, current.ImageSettings);
        Assert.Equal(7, current.FeedSettings.FeedItemCount);
        Assert.Equal("Old", retained.GeneralSettings.SiteTitle);
        Assert.False(retained.ImageSettings.EnableCDNRedirect);
        Assert.Equal(20, retained.FeedSettings.FeedItemCount);
        secondDb.ChangeTracker.Clear();
        var rows = await secondDb.BlogConfiguration.ToDictionaryAsync(row => row.CfgKey, ct);
        Assert.Equal(current.GeneralSettings.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(current.ImageSettings.ToJson(), rows[nameof(ImageSettings)].CfgValue);
        Assert.Equal(current.FeedSettings.ToJson(), rows[nameof(FeedSettings)].CfgValue);
    }

    [Fact]
    public async Task InvalidSetting_PreservesOtherRowsAndRuntime()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var config = new BlogConfig(new BlogSettingsSnapshot { GeneralSettings = General("Old"), ImageSettings = ImageSettings.DefaultValue });
        await SeedAsync(db, config.Capture().GeneralSettings);
        await SeedAsync(db, config.Capture().ImageSettings);
        var originalGeneral = config.Capture().GeneralSettings;
        var originalImage = config.Capture().ImageSettings;
        var invalid = CdnImage();
        invalid = invalid with { CDNEndpoint = "http://invalid.example.com" };

        await Assert.ThrowsAsync<ValidationException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(invalid), ct));

        Assert.Same(originalGeneral, config.Capture().GeneralSettings);
        Assert.Same(originalImage, config.Capture().ImageSettings);
        db.ChangeTracker.Clear();
        var rows = await db.BlogConfiguration.ToDictionaryAsync(row => row.CfgKey, ct);
        Assert.Equal(originalGeneral.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(originalImage.ToJson(), rows[nameof(ImageSettings)].CfgValue);
    }

    private UpdateConfigurationCommandHandler Handler(BlogDbContext db, BlogConfig config) =>
        new(db, config, Mock.Of<IEventMediator>(), _gate, NullLogger<UpdateConfigurationCommandHandler>.Instance);

    private static GeneralSettings General(string title)
    {
        var settings = GeneralSettings.DefaultValue;
        settings = settings with { SiteTitle = title };
        return settings;
    }

    private static ImageSettings CdnImage()
    {
        var settings = ImageSettings.DefaultValue;
        settings = settings with { EnableCDNRedirect = true };
        settings = settings with { CDNEndpoint = "https://cdn.example.com" };
        return settings;
    }

    private static async Task SeedAsync<T>(BlogDbContext db, T settings) where T : IBlogSettings<T>
    {
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = typeof(T).Name,
            CfgValue = settings.ToJson(),
            LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class FailingSaveInterceptor : SaveChangesInterceptor
    {
        public bool Fail { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new DbUpdateException("Save failed");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class BlockingSaveInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public TaskCompletionSource Committed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Resume { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                Enabled = false;
                Committed.SetResult();
                await Resume.Task;
            }
            return result;
        }
    }

    private sealed class CancellingSaveInterceptor(CancellationTokenSource cancellation) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled) cancellation.Cancel();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class UnregisteredSettings : IBlogSettings<UnregisteredSettings>
    {
        public static UnregisteredSettings DefaultValue => new();
    }
}
