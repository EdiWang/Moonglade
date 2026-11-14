using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Setup.Tests.TestSupport;
using Moq;
using System.Reflection;

namespace Moonglade.Setup.Tests;

public sealed class ConfigInitializerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initialize_RepairMustCompleteBeforeSettingsArePublished(bool fail)
    {
        var ct = TestContext.Current.CancellationToken;
        var persisted = StartupSettings.Defaults();
        persisted[nameof(FeedSettings)] = "not-json";
        persisted[nameof(GeneralSettings)] = (GeneralSettings.DefaultValue with { SiteTitle = "Prepared title" }).ToJson();
        persisted["LocalAccountSettings"] = "inert-legacy-json";
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .ReturnsAsync(persisted);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var write = new TaskCompletionSource<OperationCode>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), ct))
            .Returns((ICommand<OperationCode> _, CommandMediationSettings _, CancellationToken _) =>
            {
                started.SetResult();
                return write.Task;
            });
        var config = new BlogConfig();
        var retained = config.Capture();
        var initializer = new ConfigInitializer(queries.Object, commands.Object, config, NullLogger<ConfigInitializer>.Instance);

        var pending = initializer.Initialize(false, ct);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            Assert.Same(retained, config.Capture());
            Assert.Equal("inert-legacy-json", persisted["LocalAccountSettings"]);
        }
        finally
        {
            if (fail) write.SetException(new DbUpdateException("Injected repair failure."));
            else write.SetResult(OperationCode.Done);
        }

        if (fail)
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => pending);
            Assert.Same(retained, config.Capture());
        }
        else
        {
            await pending;
            Assert.Equal("Prepared title", config.Capture().GeneralSettings.SiteTitle);
            Assert.Equal(config.Capture().FeedSettings.ToJson(), persisted[nameof(FeedSettings)]);
        }
    }

    [Theory]
    [InlineData(nameof(SystemManifestSettings), "null")]
    [InlineData(nameof(SystemManifestSettings), "{}")]
    [InlineData(nameof(SystemManifestSettings), "{\"installTimeUtc\":\"0001-01-01T00:00:00Z\"}")]
    [InlineData(nameof(SystemManifestSettings), "{\"installTimeUtc\":\"2024-01-01T00:00:00\"}")]
    public async Task Initialize_InvalidManifest_DoesNotPersistAnyDefaults(string key, string invalidJson)
    {
        var ct = TestContext.Current.CancellationToken;
        var persisted = StartupSettings.Defaults();
        persisted[nameof(FeedSettings)] = "not-json";
        persisted[key] = invalidJson;
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .ReturnsAsync(persisted);
        var commands = new RecordingCommandMediator();
        var config = new BlogConfig();
        var originalGeneral = config.Capture().GeneralSettings;
        var initializer = new ConfigInitializer(queryMediator.Object, commands, config, NullLogger<ConfigInitializer>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.Initialize(isNew: false, ct));

        Assert.Empty(commands.Commands);
        Assert.Equal(invalidJson, persisted[key]);
        Assert.Same(originalGeneral, config.Capture().GeneralSettings);
        Assert.DoesNotContain("private-marker", exception.ToString());
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"feedItemCount\":\"invalid\"}")]
    [InlineData(null)]
    [InlineData("{\"feedItemCount\":\"private-marker\"}")]
    [InlineData("{\"feedItemCount\":-1,\"useFullContent\":true}")]
    public async Task Initialize_InvalidPersistedSetting_RepairsOnlyThatRowAndPreservesValidSettings(string invalidJson)
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new BlogDbContext(options);
        var persisted = StartupSettings.Defaults();
        var general = GeneralSettings.DefaultValue;
        general = general with { SiteTitle = "Preserved title" };
        persisted[nameof(GeneralSettings)] = general.ToJson();
        persisted[nameof(FeedSettings)] = invalidJson;
        var originalTimestamp = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.AddRange(persisted.Select(pair => new BlogConfigurationEntity
        {
            CfgKey = pair.Key,
            CfgValue = pair.Value,
            LastModifiedTimeUtc = originalTimestamp
        }));
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .Returns(async () => (IDictionary<string, string>)await db.BlogConfiguration.AsNoTracking()
                .ToDictionaryAsync(row => row.CfgKey, row => row.CfgValue, ct));
        var repairLogger = new RecordingLogger<AddDefaultConfigurationCommandHandler>();
        var defaultsHandler = new AddDefaultConfigurationCommandHandler(db, repairLogger);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), ct))
            .Returns((ICommand<OperationCode> command, CommandMediationSettings _, CancellationToken token) =>
                defaultsHandler.HandleAsync((AddDefaultConfigurationCommand)command, token));
        var config = new BlogConfig();
        var initializer = new ConfigInitializer(queryMediator.Object, commands.Object, config, NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew: false, ct);

        db.ChangeTracker.Clear();
        var saved = await db.BlogConfiguration.ToListAsync(ct);
        Assert.Equal(11, saved.Count);
        var repaired = Assert.Single(saved, row => row.CfgKey == nameof(FeedSettings));
        Assert.Equal(FeedSettings.DefaultValue.ToJson(), repaired.CfgValue);
        Assert.Equal(config.Capture().FeedSettings.ToJson(), repaired.CfgValue);
        Assert.True(repaired.LastModifiedTimeUtc > originalTimestamp);
        foreach (var row in saved.Where(row => row.CfgKey != nameof(FeedSettings)))
        {
            Assert.Equal(persisted[row.CfgKey], row.CfgValue);
            Assert.Equal(originalTimestamp, row.LastModifiedTimeUtc);
        }
        await initializer.Initialize(isNew: false, ct);
        commands.Verify(m => m.SendAsync(It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), ct), Times.Once);
        Assert.Single(repairLogger.Entries, entry => entry.Level == LogLevel.Warning
            && entry.Message.Contains(nameof(FeedSettings)));
        Assert.DoesNotContain(repairLogger.Entries, entry => entry.Message.Contains("private-marker"));
    }

    [Theory]
    [InlineData(nameof(GeneralSettings))]
    [InlineData(nameof(ContentSettings))]
    [InlineData(nameof(CommentSettings))]
    [InlineData(nameof(NotificationSettings))]
    [InlineData(nameof(FeedSettings))]
    [InlineData(nameof(ImageSettings))]
    [InlineData(nameof(AppearanceSettings))]
    [InlineData(nameof(AdvancedSettings))]
    [InlineData(nameof(SEOSettings))]
    [InlineData(nameof(CustomMenuSettings))]
    public async Task Initialize_PartialOrdinaryGroup_LoadsDefaultsWithoutRewritingJsonOrTimestamp(string name)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var saved = StartupSettings.Defaults();
        saved[name] = """ { "futureField": { "value": "preserved" } } """;
        var timestamp = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.AddRange(saved.Select(pair => new BlogConfigurationEntity
        {
            CfgKey = pair.Key,
            CfgValue = pair.Value,
            LastModifiedTimeUtc = timestamp
        }));
        await db.SaveChangesAsync(ct);
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .Returns(async () => (IDictionary<string, string>)await db.BlogConfiguration.AsNoTracking()
                .ToDictionaryAsync(row => row.CfgKey, row => row.CfgValue, ct));
        var commands = new Mock<ICommandMediator>(MockBehavior.Strict);
        var config = new BlogConfig();
        var logger = new RecordingLogger<ConfigInitializer>();
        var initializer = new ConfigInitializer(queries.Object, commands.Object, config, logger);
        var property = typeof(BlogSettingsSnapshot).GetProperty(name)!;
        var expected = property.PropertyType.GetProperty("DefaultValue", BindingFlags.Public | BindingFlags.Static)!.GetValue(null).ToJson();

        await initializer.Initialize(isNew: false, ct);
        await initializer.Initialize(isNew: false, ct);

        Assert.Equal(expected, property.GetValue(config.Capture()).ToJson());
        db.ChangeTracker.Clear();
        foreach (var row in await db.BlogConfiguration.ToListAsync(ct))
        {
            Assert.Equal(saved[row.CfgKey], row.CfgValue);
            Assert.Equal(timestamp, row.LastModifiedTimeUtc);
        }
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Warning);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Initialize_LoadsPersistedSettingsAndWritesMissingDefaults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var persistedSettings = new Dictionary<string, string>
        {
            [nameof(SystemManifestSettings)] = new SystemManifestSettings
            {
                VersionString = "16.3.0",
                InstallTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }.ToJson()
        };
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator
            .Setup(x => x.QueryAsync(
                It.IsAny<ListConfigurationsQuery>(),
                It.IsAny<QueryMediationSettings>(),
                cancellationToken))
            .ReturnsAsync(persistedSettings);
        var commandMediator = new RecordingCommandMediator();
        var blogConfig = new BlogConfig();
        var initializer = new ConfigInitializer(
            queryMediator.Object,
            commandMediator,
            blogConfig,
            NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew: false, cancellationToken);

        Assert.Equal("16.3.0", SystemManifestSettings.ParseStored(persistedSettings[nameof(SystemManifestSettings)]).VersionString);
        Assert.Equal(10, commandMediator.Commands.Count);
        Assert.DoesNotContain(
            commandMediator.Commands.OfType<AddDefaultConfigurationCommand>(),
            command => command.CfgKey == nameof(SystemManifestSettings));
        foreach (var command in commandMediator.Commands.OfType<AddDefaultConfigurationCommand>())
        {
            var runtimeValue = typeof(BlogSettingsSnapshot).GetProperty(command.CfgKey)!.GetValue(blogConfig.Capture());
            Assert.Equal(runtimeValue.ToJson(), command.DefaultJson);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initialize_EmptyStore_PersistsTheSameManifestAsRuntime(bool isNew)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(mediator => mediator.QueryAsync(
                It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), cancellationToken))
            .ReturnsAsync(isNew ? new Dictionary<string, string>() : new Dictionary<string, string>
            {
                [nameof(ContentSettings)] = ContentSettings.DefaultValue.ToJson()
            });
        var commandMediator = new RecordingCommandMediator();
        var config = new BlogConfig();
        var initializer = new ConfigInitializer(queryMediator.Object, commandMediator, config, NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew, cancellationToken);

        var commands = commandMediator.Commands.OfType<AddDefaultConfigurationCommand>().ToArray();
        Assert.Equal(typeof(BlogSettingsSnapshot).GetProperties().Length + 1 - (isNew ? 0 : 1), commands.Length);
        var manifestCommand = Assert.Single(commands, command => command.CfgKey == nameof(SystemManifestSettings));
        var savedManifest = SystemManifestSettings.ParseStored(manifestCommand.DefaultJson);
        Assert.Equal(DateTimeKind.Utc, savedManifest.InstallTimeUtc.Kind);
        Assert.NotEqual(default, savedManifest.InstallTimeUtc);
        Assert.Equal(
            isNew ? SystemManifestSettings.DefaultValueNew.VersionString : "0.0.0.0",
            savedManifest.VersionString);
    }

    [Fact]
    public async Task Initialize_CompleteStore_DoesNotWriteDefaults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var persisted = StartupSettings.Defaults();
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(mediator => mediator.QueryAsync(
                It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), cancellationToken))
            .ReturnsAsync(persisted);
        var commandMediator = new RecordingCommandMediator();
        var initializer = new ConfigInitializer(queryMediator.Object, commandMediator, new BlogConfig(), NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew: false, cancellationToken);

        Assert.Empty(commandMediator.Commands);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Initialize_LegacyAccountJson_IsIgnoredAndPreservedWhileOrdinarySettingsLoad(bool malformed)
    {
        var ct = TestContext.Current.CancellationToken;
        var persisted = StartupSettings.Defaults();
        var legacy = malformed ? "not-json-private-marker" : """{"username":"old-admin","passwordHash":"private-marker"}""";
        persisted["LocalAccountSettings"] = legacy;
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .ReturnsAsync(persisted);
        var commands = new RecordingCommandMediator();
        var config = new BlogConfig();
        var initializer = new ConfigInitializer(queries.Object, commands, config, NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(false, ct);

        Assert.Equal(legacy, persisted["LocalAccountSettings"]);
        Assert.Equal(GeneralSettings.DefaultValue.ToJson(), config.Capture().GeneralSettings.ToJson());
        Assert.Empty(commands.Commands);
    }

    [Fact]
    public async Task Initialize_ExistingStoreWithoutOrdinarySettingsOrManifest_FailsBeforeRepair()
    {
        var ct = TestContext.Current.CancellationToken;
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .ReturnsAsync(new Dictionary<string, string> { ["LocalAccountSettings"] = "inert-legacy-json" });
        var commands = new RecordingCommandMediator();
        var config = new BlogConfig();
        var retained = config.Capture();
        var initializer = new ConfigInitializer(queries.Object, commands, config, NullLogger<ConfigInitializer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.Initialize(false, ct));

        Assert.Empty(commands.Commands);
        Assert.Same(retained, config.Capture());
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

        public Task<MediationResult> TrySendAsync(ICommand command, CommandMediationSettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationResult<TCommandResult>> TrySendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<MediationDecision> EvaluateAsync(ICommand command, CommandMediationSettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task SendAsync(
            ICommand command,
            CommandMediationSettings settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.CompletedTask;
        }

        public Task<TCommandResult> SendAsync<TCommandResult>(
            ICommand<TCommandResult> command,
            CommandMediationSettings settings,
            CancellationToken cancellationToken)
        {
            Commands.Add(command);
            return Task.FromResult(default(TCommandResult)!);
        }
    }
}
