using System.Collections.Concurrent;
using System.Reflection;
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

    [Fact]
    public async Task AddViewCountCommand_RemovesPostLockAfterUpdate()
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

        var result = await handler.HandleAsync(new AddViewCountCommand(postId), TestContext.Current.CancellationToken);

        Assert.Equal(5, result);
        Assert.False(GetLocks(typeof(AddViewCountCommandHandler)).ContainsKey(postId));
    }

    [Fact]
    public async Task AddRequestCountCommand_RemovesPostLockAfterUpdate()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var handler = new AddRequestCountCommandHandler(db, Mock.Of<ILogger<AddRequestCountCommandHandler>>());

        var result = await handler.HandleAsync(new AddRequestCountCommand(postId), TestContext.Current.CancellationToken);

        Assert.Equal(1, result);
        Assert.False(GetLocks(typeof(AddRequestCountCommandHandler)).ContainsKey(postId));
    }

    private static ConcurrentDictionary<Guid, SemaphoreSlim> GetLocks(Type handlerType)
    {
        var field = handlerType.GetField("_locks", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Lock field was not found.");

        return (ConcurrentDictionary<Guid, SemaphoreSlim>)(field.GetValue(null)
            ?? throw new InvalidOperationException("Lock field was not initialized."));
    }
}
