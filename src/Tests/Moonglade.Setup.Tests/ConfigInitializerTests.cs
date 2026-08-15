using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moq;

namespace Moonglade.Setup.Tests;

public sealed class ConfigInitializerTests
{
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
    }

    private sealed class RecordingCommandMediator : ICommandMediator
    {
        public List<ICommand> Commands { get; } = [];

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
