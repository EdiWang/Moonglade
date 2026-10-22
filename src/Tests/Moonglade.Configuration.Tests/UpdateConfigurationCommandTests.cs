using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration.Tests;

public class UpdateConfigurationCommandTests
{
    [Fact]
    public async Task Save_UpdatesDatabaseAndRuntimeFromDetachedSnapshot()
    {
        using var db = CreateDbContext();
        var config = new BlogConfig { GeneralSettings = new GeneralSettings { SiteTitle = "Old" } };
        await SeedAsync(db, config.GeneralSettings);
        var original = config.GeneralSettings;
        var model = new GeneralSettings { SiteTitle = "New", Description = "Description" };
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
        var settings = new GeneralSettings { SiteTitle = "Persisted" };
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
        var config = new BlogConfig { GeneralSettings = new GeneralSettings { AvatarUrl = "old.png" } };
        await SeedAsync(db, config.GeneralSettings);
        var originalGeneral = config.GeneralSettings;
        var originalImage = config.ImageSettings;
        var command = UpdateConfigurationCommand.Create(
            new GeneralSettings { AvatarUrl = "new.png" }, new ImageSettings { EnableCDNRedirect = true });

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

        await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(
            new GeneralSettings { AvatarUrl = "https://cdn.example.com/avatar.png" },
            new ImageSettings { EnableCDNRedirect = true, CDNEndpoint = "https://cdn.example.com" }),
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

        await Assert.ThrowsAsync<DbUpdateException>(() => Handler(db, config).HandleAsync(
            UpdateConfigurationCommand.Create(new LocalAccountSettings { Username = "Changed", IsTotpEnabled = true }),
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
        var command = UpdateConfigurationCommand.Create(new GeneralSettings { SiteTitle = "New" });

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
            new GeneralSettings { SiteTitle = "First" }), TestContext.Current.CancellationToken);
        await interceptor.Committed.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = Handler(secondDb, config).HandleAsync(UpdateConfigurationCommand.Create(
            new GeneralSettings { SiteTitle = "Second" }), TestContext.Current.CancellationToken);
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

        await Handler(db, config).HandleAsync(UpdateConfigurationCommand.Create(
            new GeneralSettings { SiteTitle = "Committed" }), cancellation.Token);

        Assert.True(cancellation.IsCancellationRequested);
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

    private static UpdateConfigurationCommandHandler Handler(BlogDbContext db, IBlogConfig config) =>
        new(db, config, NullLogger<UpdateConfigurationCommandHandler>.Instance);

    private static async Task SeedAsync<T>(BlogDbContext db, T settings) where T : IBlogSettings<T>
    {
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = typeof(T).Name, CfgValue = settings.ToJson(), LastModifiedTimeUtc = DateTime.UtcNow.AddDays(-1)
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
