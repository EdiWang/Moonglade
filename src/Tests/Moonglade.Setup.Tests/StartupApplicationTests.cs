using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Setup.Tests.TestSupport;
using Moq;

namespace Moonglade.Setup.Tests;

[Collection("Startup site icon cache")]
public class StartupApplicationTests
{
    [Theory]
    [InlineData("empty-settings")]
    [InlineData("invalid-manifest")]
    [InlineData("invalid-manifest-time")]
    public async Task InitStartUp_WhenInitializationFails_ThrowsWithoutStartingHostedServicesOrChangingData(string scenario)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var scheduledTime = DateTime.UtcNow.AddMinutes(-1);
        var post = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Preserved scheduled post",
            PostStatus = PostStatus.Scheduled,
            ScheduledPublishTimeUtc = scheduledTime,
            CreateTimeUtc = scheduledTime.AddDays(-1)
        };
        db.Post.Add(post);
        if (scenario != "empty-settings")
            db.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = nameof(FeedSettings),
                CfgValue = "not-json",
                LastModifiedTimeUtc = scheduledTime
            });
        if (scenario.StartsWith("invalid-manifest", StringComparison.Ordinal))
            db.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = nameof(SystemManifestSettings),
                CfgValue = scenario == "invalid-manifest" ? "null" : """{"installTimeUtc":"0001-01-01T00:00:00Z"}""",
                LastModifiedTimeUtc = scheduledTime
            });
        await db.SaveChangesAsync(ct);
        var originalRows = await db.BlogConfiguration.AsNoTracking().OrderBy(row => row.CfgKey).ToListAsync(ct);
        using var harness = new StartupTestHarness(db);
        var originalRuntime = harness.Config.Capture().GeneralSettings;
        var hosted = new Mock<IHostedService>();
        var logger = new RecordingLogger<StartupApplicationTests>();
        await using var app = BuildApplication(harness.Initializer, hosted.Object, logger);
        hosted.Setup(service => service.StartAsync(It.IsAny<CancellationToken>()))
            .Callback(() => app.Lifetime.StopApplication())
            .Returns(Task.CompletedTask);
        hosted.Setup(service => service.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            app.InitStartUp().WaitAsync(TimeSpan.FromSeconds(10), ct));

        Assert.Contains("Error initializing blog configuration", exception.Message);
        Assert.Contains("RESTART", exception.Message);
        hosted.Verify(service => service.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.False(app.Lifetime.ApplicationStarted.IsCancellationRequested);
        Assert.Same(originalRuntime, harness.Config.Capture().GeneralSettings);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Critical && entry.Message == exception.Message);
        db.ChangeTracker.Clear();
        var saved = await db.Post.SingleAsync(ct);
        Assert.Equal(post.Id, saved.Id);
        Assert.Equal("Preserved scheduled post", saved.Title);
        Assert.Equal(PostStatus.Scheduled, saved.PostStatus);
        Assert.Equal(scheduledTime, saved.ScheduledPublishTimeUtc);
        Assert.Null(saved.PubDateUtc);
        Assert.Null(saved.RouteLink);
        var rows = await db.BlogConfiguration.OrderBy(row => row.CfgKey).ToListAsync(ct);
        Assert.Equal(originalRows.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)),
            rows.Select(row => (row.CfgKey, row.CfgValue, row.LastModifiedTimeUtc)));
    }

    [Fact]
    public async Task InitStartUp_WhenInitializationSucceeds_ReturnsBeforeCallerStartsTheHost()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateContext();
        var settings = StartupSettings.Defaults();
        var timestamp = DateTime.UtcNow.AddDays(-1);
        db.BlogConfiguration.AddRange(settings.Select(pair => new BlogConfigurationEntity
        {
            CfgKey = pair.Key,
            CfgValue = pair.Value,
            LastModifiedTimeUtc = timestamp
        }));
        await db.SaveChangesAsync(ct);
        using var harness = new StartupTestHarness(db);
        var hosted = new Mock<IHostedService>();
        hosted.Setup(service => service.StartAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        hosted.Setup(service => service.StopAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var logger = new RecordingLogger<StartupApplicationTests>();
        await using var app = BuildApplication(harness.Initializer, hosted.Object, logger);

        await app.InitStartUp().WaitAsync(TimeSpan.FromSeconds(10), ct);

        hosted.Verify(service => service.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(settings[nameof(GeneralSettings)], harness.Config.Capture().GeneralSettings.ToJson());
        Assert.False(await db.OwnerAccount.AnyAsync(ct));
        Assert.DoesNotContain(logger.Entries, entry => entry.Level == LogLevel.Critical);
        db.ChangeTracker.Clear();
        var rows = await db.BlogConfiguration.ToDictionaryAsync(row => row.CfgKey, ct);
        Assert.Equal(settings.Count, rows.Count);
        foreach (var (key, json) in settings)
        {
            Assert.Equal(json, rows[key].CfgValue);
            Assert.Equal(timestamp, rows[key].LastModifiedTimeUtc);
        }
        await app.StartAsync(ct);
        hosted.Verify(service => service.StartAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(app.Lifetime.ApplicationStarted.IsCancellationRequested);
        await app.StopAsync(ct);
    }

    private static BlogDbContext CreateContext() => new(new DbContextOptionsBuilder<BlogDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static WebApplication BuildApplication(StartUpInitializer initializer, IHostedService hosted,
        RecordingLogger<StartupApplicationTests> logger)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var provider = new Mock<ILoggerProvider>();
        provider.Setup(value => value.CreateLogger(It.IsAny<string>())).Returns(logger);
        builder.Logging.AddProvider(provider.Object);
        builder.Services.AddSingleton(initializer);
        builder.Services.AddSingleton(hosted);
        return builder.Build();
    }
}
