using Microsoft.EntityFrameworkCore;
using LiteBus.Events.Abstractions;
using Moq;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration.Tests;

public class UpdateConfigurationCommandTests
{
    [Fact]
    public async Task Save_UpdatesDatabaseAndRuntimeFromDetachedSnapshot()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig { GeneralSettings = General("Old") };
        await SeedAsync(db, config.GeneralSettings);
        var original = config.GeneralSettings;
        var model = General("New");
        model.Description = "Description";
        var command = UpdateConfigurationCommand.Create(model);
        model.SiteTitle = "Changed after dispatch";

        await Handler(db, config).HandleAsync(command, TestContext.Current.CancellationToken);

        Assert.Equal("New", config.GeneralSettings.SiteTitle);
        Assert.Equal("Old", original.SiteTitle);
        Assert.NotSame(model, config.GeneralSettings);
        Assert.Equal(config.GeneralSettings.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
        model.Description = "Changed after save";
        Assert.Equal("Description", config.GeneralSettings.Description);
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
        Assert.Equal("Persisted", config.GeneralSettings.SiteTitle);
    }

    [Fact]
    public async Task MissingPairedSetting_ChangesNeitherDatabaseNorRuntime()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig { GeneralSettings = General("Old") };
        config.GeneralSettings.AvatarUrl = "old.png";
        await SeedAsync(db, config.GeneralSettings);
        var originalGeneral = config.GeneralSettings;
        var originalImage = config.ImageSettings;
        var general = General("New");
        general.AvatarUrl = "new.png";
        var command = UpdateConfigurationCommand.Create(general, CdnImage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(db, config)
            .HandleAsync(command, TestContext.Current.CancellationToken));

        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Same(originalImage, config.ImageSettings);
        Assert.Equal(originalGeneral.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
        Assert.False(db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task PairedSettings_AreSavedAndAppliedTogether()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig();
        await SeedAsync(db, config.GeneralSettings);
        await SeedAsync(db, config.ImageSettings);

        var general = General("New");
        general.AvatarUrl = "https://cdn.example.com/avatar.png";
        await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(general, CdnImage()),
            TestContext.Current.CancellationToken);

        Assert.True(config.ImageSettings.EnableCDNRedirect);
        Assert.Equal("https://cdn.example.com/avatar.png", config.GeneralSettings.AvatarUrl);
        var rows = await db.BlogConfiguration.ToDictionaryAsync(p => p.CfgKey, TestContext.Current.CancellationToken);
        Assert.Equal(config.GeneralSettings.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(config.ImageSettings.ToJson(), rows[nameof(ImageSettings)].CfgValue);
        Assert.Equal(rows[nameof(GeneralSettings)].LastModifiedTimeUtc, rows[nameof(ImageSettings)].LastModifiedTimeUtc);
    }

    [Fact]
    public async Task SaveFailure_PreservesRuntimeAndPersistedSettings()
    {
        var interceptor = new FailingSaveInterceptor();
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig { LocalAccountSettings = LocalAccountSettings.DefaultValue };
        await SeedAsync(db, config.LocalAccountSettings);
        var original = config.LocalAccountSettings;
        interceptor.Fail = true;

        var updated = LocalAccountSettings.DefaultValue;
        updated.Username = "Changed";
        updated.IsTotpEnabled = true;
        updated.TotpSecret = "JBSWY3DPEHPK3PXP";
        await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(updated),
            TestContext.Current.CancellationToken));

        Assert.Same(original, config.LocalAccountSettings);
        db.ChangeTracker.Clear();
        Assert.Equal(original.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task CancelledSave_PreservesRuntimeAndAllowsLaterSave()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig();
        await SeedAsync(db, config.GeneralSettings);
        var original = config.GeneralSettings;
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var command = UpdateConfigurationCommand.Create(General("New"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Handler(db, config).HandleAsync(command, cancelled.Token));

        Assert.Same(original, config.GeneralSettings);
        await Handler(db, config).HandleAsync(command, TestContext.Current.CancellationToken);
        Assert.Equal("New", config.GeneralSettings.SiteTitle);
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
        await SeedAsync(firstDb, config.GeneralSettings);
        interceptor.Enabled = true;
        var first = Handler(firstDb, config).HandleAsync(UpdateConfigurationCommand.Create(
            General("First")), TestContext.Current.CancellationToken);
        await interceptor.Committed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var second = Handler(secondDb, config).HandleAsync(UpdateConfigurationCommand.Create(
            General("Second")), TestContext.Current.CancellationToken);
        var secondWasBlocked = !second.IsCompleted;
        interceptor.Resume.SetResult();

        await Task.WhenAll(first, second);

        Assert.True(secondWasBlocked);
        Assert.Equal("Second", config.GeneralSettings.SiteTitle);
        secondDb.ChangeTracker.Clear();
        Assert.Equal(config.GeneralSettings.ToJson(), (await secondDb.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task CancellationAfterCommit_StillAppliesPersistedValue()
    {
        using var cancellation = new CancellationTokenSource();
        var interceptor = new CancellingSaveInterceptor(cancellation);
        using var db = CreateDbContext(interceptor);
        var config = new BlogConfig();
        await SeedAsync(db, config.GeneralSettings);
        interceptor.Enabled = true;
        var events = new Mock<IEventMediator>();
        events.Setup(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None))
            .Callback<IEvent, EventMediationSettings, CancellationToken>((message, _, _) =>
            {
                Assert.True(cancellation.IsCancellationRequested);
                Assert.Equal("Committed", config.GeneralSettings.SiteTitle);
                Assert.Equal(config.GeneralSettings.ToJson(), db.BlogConfiguration.Local.Single().CfgValue);
                Assert.Equal([nameof(GeneralSettings)], ((ConfigurationChangedEvent)message).Names);
            }).Returns(Task.CompletedTask);

        await new UpdateConfigurationCommandHandler(db, config, events.Object, NullLogger<UpdateConfigurationCommandHandler>.Instance)
            .HandleAsync(UpdateConfigurationCommand.Create(General("Committed")), cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
        events.Verify(m => m.PublishAsync(It.IsAny<IEvent>(), It.IsAny<EventMediationSettings>(), CancellationToken.None), Times.Once);
        Assert.Equal("Committed", config.GeneralSettings.SiteTitle);
        Assert.Equal(config.GeneralSettings.ToJson(), (await db.BlogConfiguration.SingleAsync(TestContext.Current.CancellationToken)).CfgValue);
    }

    [Fact]
    public async Task UnknownSetting_IsRejectedBeforeDatabaseChanges()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig();
        await SeedAsync(db, config.GeneralSettings);
        var original = config.GeneralSettings;

        await Assert.ThrowsAsync<InvalidOperationException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(new UnregisteredSettings()), TestContext.Current.CancellationToken));

        Assert.Same(original, config.GeneralSettings);
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
        var config = new BlogConfig { GeneralSettings = General("Old") };
        if (scenario != "missing") await SeedAsync(db, config.GeneralSettings);
        interceptor.Fail = scenario == "failed-save";
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        if (scenario == "cancelled") await cancellation.CancelAsync();
        var events = new Mock<IEventMediator>();
        var handler = new UpdateConfigurationCommandHandler(db, config, events.Object, NullLogger<UpdateConfigurationCommandHandler>.Instance);
        var settings = General(scenario == "unchanged" ? "Old" : "New");
        if (scenario == "invalid") settings.SiteTitle = null;

        var error = await Record.ExceptionAsync(() => handler.HandleAsync(UpdateConfigurationCommand.Create(settings), cancellation.Token));

        if (scenario == "unchanged") Assert.Null(error);
        else Assert.NotNull(error);
        Assert.Empty(events.Invocations);
    }

    [Fact]
    public async Task InvalidPairedSetting_DoesNotChangeEitherRowOrRuntime()
    {
        var ct = TestContext.Current.CancellationToken;
        using var db = CreateDbContext();
        var config = new BlogConfig { GeneralSettings = General("Old"), ImageSettings = ImageSettings.DefaultValue };
        await SeedAsync(db, config.GeneralSettings);
        await SeedAsync(db, config.ImageSettings);
        var originalGeneral = config.GeneralSettings;
        var originalImage = config.ImageSettings;
        var invalid = CdnImage();
        invalid.CDNEndpoint = "http://invalid.example.com";

        await Assert.ThrowsAsync<ValidationException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(General("New"), invalid), ct));

        Assert.Same(originalGeneral, config.GeneralSettings);
        Assert.Same(originalImage, config.ImageSettings);
        db.ChangeTracker.Clear();
        var rows = await db.BlogConfiguration.ToDictionaryAsync(row => row.CfgKey, ct);
        Assert.Equal(originalGeneral.ToJson(), rows[nameof(GeneralSettings)].CfgValue);
        Assert.Equal(originalImage.ToJson(), rows[nameof(ImageSettings)].CfgValue);
    }

    private static UpdateConfigurationCommandHandler Handler(BlogDbContext db, IBlogConfig config) =>
        new(db, config, Mock.Of<IEventMediator>(), NullLogger<UpdateConfigurationCommandHandler>.Instance);

    private static GeneralSettings General(string title)
    {
        var settings = GeneralSettings.DefaultValue;
        settings.SiteTitle = title;
        return settings;
    }

    private static ImageSettings CdnImage()
    {
        var settings = ImageSettings.DefaultValue;
        settings.EnableCDNRedirect = true;
        settings.CDNEndpoint = "https://cdn.example.com";
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
