using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests;

public class PostViewCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task AddViewCountCommand_IncrementsPostViewAndDailyView()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.PostView.Add(new PostViewEntity
        {
            PostId = postId,
            RequestCount = 1,
            ViewCount = 4,
            BeginTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddViewCountCommandHandler(db, Mock.Of<ILogger<AddViewCountCommandHandler>>());
        var todayUtc = DateTime.UtcNow.Date;

        var result = await handler.HandleAsync(new AddViewCountCommand(postId), TestContext.Current.CancellationToken);

        var postView = await db.PostView.SingleAsync(TestContext.Current.CancellationToken);
        var dailyView = await db.PostViewDaily.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(5, result);
        Assert.Equal(5, postView.ViewCount);
        Assert.Equal(postId, dailyView.PostId);
        Assert.Equal(todayUtc, dailyView.ViewDateUtc);
        Assert.Equal(1, dailyView.ViewCount);
    }

    [Fact]
    public async Task AddViewCountCommand_IncrementsExistingDailyView()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.PostView.Add(new PostViewEntity
        {
            PostId = postId,
            RequestCount = 1,
            ViewCount = 4,
            BeginTimeUtc = DateTime.UtcNow.AddDays(-1)
        });
        db.PostViewDaily.Add(new PostViewDailyEntity
        {
            PostId = postId,
            ViewDateUtc = DateTime.UtcNow.Date,
            ViewCount = 2
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new AddViewCountCommandHandler(db, Mock.Of<ILogger<AddViewCountCommandHandler>>());

        await handler.HandleAsync(new AddViewCountCommand(postId), TestContext.Current.CancellationToken);

        var dailyView = await db.PostViewDaily.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(3, dailyView.ViewCount);
    }
}
