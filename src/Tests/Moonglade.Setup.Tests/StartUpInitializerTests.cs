using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Setup.Tests;

public class StartUpInitializerTests
{
    [Fact]
    public async Task InitStartUpAsync_ExistingDatabase_PreservesEmptyPostContentType()
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
        var post = new PostEntity
        {
            Id = Guid.NewGuid(),
            Title = "Historical post",
            ContentType = "",
            CreateTimeUtc = DateTime.UtcNow
        };
        context.Post.Add(post);
        await context.SaveChangesAsync(cancellationToken);
        var migrationManager = new Mock<IMigrationManager>();
        migrationManager
            .Setup(manager => manager.TryMigrationAsync(context, cancellationToken))
            .ReturnsAsync(new MigrationResult(MigrationStatus.NotRequired));
        var configInitializer = new Mock<IConfigInitializer>();
        configInitializer
            .Setup(initializer => initializer.Initialize(false, cancellationToken))
            .Returns(Task.CompletedTask);
        var siteIconBuilder = new Mock<ISiteIconBuilder>();
        siteIconBuilder.Setup(builder => builder.GenerateSiteIcons()).Returns(Task.CompletedTask);
        var initializer = new StartUpInitializer(
            NullLogger<StartUpInitializer>.Instance,
            context,
            configInitializer.Object,
            migrationManager.Object,
            siteIconBuilder.Object);

        var result = await initializer.InitStartUpAsync(cancellationToken);

        Assert.Equal(InitStartUpResult.Success, result);
        context.ChangeTracker.Clear();
        Assert.Empty((await context.Post.SingleAsync(cancellationToken)).ContentType);
        migrationManager.Verify(manager => manager.TryMigrationAsync(context, cancellationToken), Times.Once);
        configInitializer.Verify(config => config.Initialize(false, cancellationToken), Times.Once);
    }
}
