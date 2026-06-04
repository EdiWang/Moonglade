using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Configuration.Tests;

public class ListConfigurationsQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAllConfigurationKeyValues()
    {
        using var db = CreateDbContext();
        db.BlogConfiguration.AddRange(
            new BlogConfigurationEntity { CfgKey = "GeneralSettings", CfgValue = "{\"SiteTitle\":\"Test\"}", LastModifiedTimeUtc = DateTime.UtcNow },
            new BlogConfigurationEntity { CfgKey = "FeedSettings", CfgValue = "{\"FeedItemCount\":10}", LastModifiedTimeUtc = DateTime.UtcNow });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = new ListConfigurationsQueryHandler(db);

        var result = await handler.HandleAsync(new ListConfigurationsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("{\"SiteTitle\":\"Test\"}", result["GeneralSettings"]);
        Assert.Equal("{\"FeedItemCount\":10}", result["FeedSettings"]);
    }

    [Fact]
    public async Task HandleAsync_EmptyTable_ReturnsEmptyDictionary()
    {
        using var db = CreateDbContext();
        var handler = new ListConfigurationsQueryHandler(db);

        var result = await handler.HandleAsync(new ListConfigurationsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoTrackingResults()
    {
        using var db = CreateDbContext();
        db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = "GeneralSettings",
            CfgValue = "{}",
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.ChangeTracker.Clear();
        var handler = new ListConfigurationsQueryHandler(db);

        await handler.HandleAsync(new ListConfigurationsQuery(), TestContext.Current.CancellationToken);

        Assert.Empty(db.ChangeTracker.Entries());
    }
}
