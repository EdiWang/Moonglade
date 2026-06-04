using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Moonglade.BackgroundServices.Tests;

public class UpdateCheckServiceTests
{
    [Fact]
    public async Task StartAsync_UpdateCheckDisabled_DoesNotCallGitHubClient()
    {
        var gitHubClient = new Mock<IGitHubReleaseClient>();
        var service = new UpdateCheckService(
            gitHubClient.Object,
            new UpdateCheckerState(),
            CreateConfiguration(new Dictionary<string, string?> { ["EnableUpdateCheck"] = "false" }),
            Mock.Of<ILogger<UpdateCheckService>>());

        await service.StartAsync(TestContext.Current.CancellationToken);

        gitHubClient.Verify(x => x.GetLatestReleaseTagAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_InvalidCron_DoesNotCallGitHubClient()
    {
        var gitHubClient = new Mock<IGitHubReleaseClient>();
        var service = new UpdateCheckService(
            gitHubClient.Object,
            new UpdateCheckerState(),
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["EnableUpdateCheck"] = "true",
                ["UpdateCheckCron"] = "not-a-cron"
            }),
            Mock.Of<ILogger<UpdateCheckService>>());

        await service.StartAsync(TestContext.Current.CancellationToken);

        gitHubClient.Verify(x => x.GetLatestReleaseTagAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void UpdateCheckerState_SetNewVersion_UpdatesState()
    {
        var state = new UpdateCheckerState();

        state.SetNewVersion("v99.0.0");

        Assert.True(state.HasNewVersion);
        Assert.Equal("v99.0.0", state.NewVersion);
    }

    [Fact]
    public void UpdateCheckerState_SetNewVersionNull_ClearsState()
    {
        var state = new UpdateCheckerState();
        state.SetNewVersion("v99.0.0");

        state.SetNewVersion(null);

        Assert.False(state.HasNewVersion);
        Assert.Null(state.NewVersion);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
