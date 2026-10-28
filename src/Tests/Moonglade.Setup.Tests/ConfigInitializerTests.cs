using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Setup.Tests;

public sealed class ConfigInitializerTests
{
    [Theory]
    [InlineData(nameof(LocalAccountSettings))]
    [InlineData(nameof(SystemManifestSettings))]
    public async Task Initialize_InvalidAccountOrManifest_DoesNotPersistAnyDefaults(string key)
    {
        var ct = TestContext.Current.CancellationToken;
        var persisted = new BlogConfig().LoadFromConfig(new Dictionary<string, string>(), isNew: true).ToDictionary();
        persisted[nameof(FeedSettings)] = "not-json";
        persisted[key] = "null";
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), ct))
            .ReturnsAsync(persisted);
        var commands = new RecordingCommandMediator();
        var initializer = new ConfigInitializer(queryMediator.Object, commands, new BlogConfig(), NullLogger<ConfigInitializer>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => initializer.Initialize(isNew: false, ct));

        Assert.Empty(commands.Commands);
        Assert.Equal("null", persisted[key]);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"feedItemCount\":\"invalid\"}")]
    public async Task Initialize_InvalidPersistedSetting_RepairsOnlyThatRowAndPreservesValidSettings(string invalidJson)
    {
        var ct = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<BlogDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new BlogDbContext(options);
        var persisted = new BlogConfig().LoadFromConfig(new Dictionary<string, string>(), isNew: true).ToDictionary();
        persisted[nameof(GeneralSettings)] = new GeneralSettings { SiteTitle = "Preserved title" }.ToJson();
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
        var defaultsHandler = new AddDefaultConfigurationCommandHandler(db, NullLogger<AddDefaultConfigurationCommandHandler>.Instance);
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
        Assert.Equal(config.FeedSettings.ToJson(), repaired.CfgValue);
        Assert.True(repaired.LastModifiedTimeUtc > originalTimestamp);
        foreach (var row in saved.Where(row => row.CfgKey != nameof(FeedSettings)))
        {
            Assert.Equal(persisted[row.CfgKey], row.CfgValue);
            Assert.Equal(originalTimestamp, row.LastModifiedTimeUtc);
        }
        await initializer.Initialize(isNew: false, ct);
        commands.Verify(m => m.SendAsync(It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), ct), Times.Once);
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

        Assert.Equal("16.3.0", blogConfig.SystemManifestSettings.VersionString);
        Assert.Equal(10, commandMediator.Commands.Count);
        Assert.DoesNotContain(
            commandMediator.Commands.OfType<AddDefaultConfigurationCommand>(),
            command => command.CfgKey == nameof(SystemManifestSettings));
        foreach (var command in commandMediator.Commands.OfType<AddDefaultConfigurationCommand>())
        {
            var runtimeValue = typeof(IBlogConfig).GetProperty(command.CfgKey)!.GetValue(blogConfig);
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
            .ReturnsAsync(new Dictionary<string, string>());
        var commandMediator = new RecordingCommandMediator();
        var config = new BlogConfig();
        var initializer = new ConfigInitializer(queryMediator.Object, commandMediator, config, NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew, cancellationToken);

        var commands = commandMediator.Commands.OfType<AddDefaultConfigurationCommand>().ToArray();
        Assert.Equal(typeof(IBlogConfig).GetProperties().Length, commands.Length);
        var manifestCommand = Assert.Single(commands, command => command.CfgKey == nameof(SystemManifestSettings));
        Assert.Equal(config.SystemManifestSettings.ToJson(), manifestCommand.DefaultJson);
        Assert.Equal(
            isNew ? SystemManifestSettings.DefaultValueNew.VersionString : "0.0.0.0",
            config.SystemManifestSettings.VersionString);
    }

    [Fact]
    public async Task Initialize_CompleteStore_DoesNotWriteDefaults()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var persisted = new BlogConfig().LoadFromConfig(new Dictionary<string, string>(), isNew: true).ToDictionary();
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator.Setup(mediator => mediator.QueryAsync(
                It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), cancellationToken))
            .ReturnsAsync(persisted);
        var commandMediator = new RecordingCommandMediator();
        var initializer = new ConfigInitializer(queryMediator.Object, commandMediator, new BlogConfig(), NullLogger<ConfigInitializer>.Instance);

        await initializer.Initialize(isNew: false, cancellationToken);

        Assert.Empty(commandMediator.Commands);
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
