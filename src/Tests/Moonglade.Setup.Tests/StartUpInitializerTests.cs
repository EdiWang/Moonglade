using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Setup.Tests;

[Collection("Startup site icon cache")]
public class StartUpInitializerTests : IDisposable
{
    private readonly string _testTempDirectory = Path.Combine(Path.GetTempPath(), "moonglade-startup-tests", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _originalTempVariables = new[] { "TEMP", "TMP", "TMPDIR" }
        .ToDictionary(name => name, Environment.GetEnvironmentVariable);

    public StartUpInitializerTests()
    {
        Directory.CreateDirectory(_testTempDirectory);
        foreach (var name in _originalTempVariables.Keys)
        {
            Environment.SetEnvironmentVariable(name, _testTempDirectory);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("16.7.0")]
    public async Task InitStartUpAsync_ExistingDatabase_PreservesContentAndManifest(string manifestVersion)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new BlogDbContext(options);
        context.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "ContentSettings",
            CfgValue = "{}",
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        var manifest = new SystemManifestSettings
        {
            VersionString = manifestVersion,
            InstallTimeUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var manifestJson = manifest.ToJson();
        if (manifestVersion is not null)
        {
            context.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = nameof(SystemManifestSettings),
                CfgValue = manifestJson,
                LastModifiedTimeUtc = manifest.InstallTimeUtc
            });
        }
        var post = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Historical post",
            ContentType = "",
            CreateTimeUtc = DateTime.UtcNow
        };
        context.Post.Add(post);
        await context.SaveChangesAsync(cancellationToken);
        var queryMediator = new Mock<IQueryMediator>();
        queryMediator
            .Setup(mediator => mediator.QueryAsync(
                It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), cancellationToken))
            .Returns(async () => (IDictionary<string, string>)await context.BlogConfiguration
                .ToDictionaryAsync(row => row.CfgKey, row => row.CfgValue, cancellationToken));
        var commandMediator = new Mock<ICommandMediator>();
        var addDefaultHandler = new AddDefaultConfigurationCommandHandler(context, NullLogger<AddDefaultConfigurationCommandHandler>.Instance);
        commandMediator
            .Setup(mediator => mediator.SendAsync(
                It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), cancellationToken))
            .Returns((ICommand<OperationCode> command, CommandMediationSettings _, CancellationToken ct) =>
                addDefaultHandler.HandleAsync((AddDefaultConfigurationCommand)command, ct));
        var configInitializer = new ConfigInitializer(
            queryMediator.Object, commandMediator.Object, new BlogConfig(), NullLogger<ConfigInitializer>.Instance);
        var webEnvironment = new Mock<IWebHostEnvironment>();
        webEnvironment.SetupGet(environment => environment.WebRootPath).Returns(Path.GetTempPath());
        var siteIconBuilder = new SiteIconBuilder(
            NullLogger<SiteIconBuilder>.Instance, queryMediator.Object, webEnvironment.Object);
        var initializer = new StartUpInitializer(
            NullLogger<StartUpInitializer>.Instance,
            context,
            configInitializer,
            siteIconBuilder);

        var result = await initializer.InitStartUpAsync(cancellationToken);

        Assert.Equal(InitStartUpResult.Success, result);
        context.ChangeTracker.Clear();
        Assert.Empty((await context.Post.SingleAsync(cancellationToken)).ContentType);
        Assert.Equal(11, await context.BlogConfiguration.CountAsync(cancellationToken));
        if (manifestVersion is not null)
        {
            var savedManifest = await context.BlogConfiguration.SingleAsync(
                row => row.CfgKey == nameof(SystemManifestSettings), cancellationToken);
            Assert.Equal(manifestJson, savedManifest.CfgValue);
            Assert.Equal(manifest.InstallTimeUtc, savedManifest.LastModifiedTimeUtc);
        }
        queryMediator.Verify(mediator => mediator.QueryAsync(
            It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), cancellationToken), Times.Once);
    }

    public void Dispose()
    {
        foreach (var (name, value) in _originalTempVariables)
        {
            Environment.SetEnvironmentVariable(name, value);
        }
        Directory.Delete(_testTempDirectory, recursive: true);
    }
}

[CollectionDefinition("Startup site icon cache", DisableParallelization = true)]
public class StartupSiteIconCacheCollection
{
}
