using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests;

public class PostStatusCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task DeletePostCommand_SoftDelete_SetsIsDeleted()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeletePostCommandHandler(db, Mock.Of<ILogger<DeletePostCommandHandler>>());

        await handler.HandleAsync(new DeletePostCommand(postId, SoftDelete: true), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.True(post.IsDeleted);
    }

    [Fact]
    public async Task DeletePostCommand_HardDelete_RemovesPost()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeletePostCommandHandler(db, Mock.Of<ILogger<DeletePostCommandHandler>>());

        await handler.HandleAsync(new DeletePostCommand(postId), TestContext.Current.CancellationToken);

        Assert.Empty(await db.Post.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestorePostCommand_SetsIsDeletedFalse()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var post = CreatePostEntity(postId, PostStatus.Published, "deleted-post", DateTime.UtcNow.AddDays(-1));
        post.IsDeleted = true;
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new RestorePostCommandHandler(db, Mock.Of<ILogger<RestorePostCommandHandler>>());

        await handler.HandleAsync(new RestorePostCommand(postId), TestContext.Current.CancellationToken);

        var restoredPost = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.False(restoredPost.IsDeleted);
    }

    [Fact]
    public async Task UnpublishPostCommand_ClearsPublishStateAndRouteLink()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UnpublishPostCommandHandler(db, Mock.Of<ILogger<UnpublishPostCommandHandler>>());
        var before = DateTime.UtcNow;

        await handler.HandleAsync(new UnpublishPostCommand(postId), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Draft, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.ScheduledPublishTimeUtc);
        Assert.Null(post.RouteLink);
        Assert.NotNull(post.LastModifiedUtc);
        Assert.InRange(post.LastModifiedUtc.Value, before, after);
    }

    [Fact]
    public async Task CancelScheduleCommand_ScheduledPost_ReturnsPostToDraft()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var post = CreatePostEntity(postId, PostStatus.Scheduled, "scheduled-post");
        post.ScheduledPublishTimeUtc = DateTime.UtcNow.AddHours(1);
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelScheduleCommandHandler(db, Mock.Of<ILogger<CancelScheduleCommandHandler>>());
        var before = DateTime.UtcNow;

        await handler.HandleAsync(new CancelScheduleCommand(postId), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        var savedPost = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Draft, savedPost.PostStatus);
        Assert.Null(savedPost.ScheduledPublishTimeUtc);
        Assert.Null(savedPost.PubDateUtc);
        Assert.Null(savedPost.RouteLink);
        Assert.NotNull(savedPost.LastModifiedUtc);
        Assert.InRange(savedPost.LastModifiedUtc.Value, before, after);
    }

    [Fact]
    public async Task CancelScheduleCommand_NonScheduledPost_DoesNotModifyPost()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var pubDate = DateTime.UtcNow.AddDays(-1);
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", pubDate));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CancelScheduleCommandHandler(db, Mock.Of<ILogger<CancelScheduleCommandHandler>>());

        await handler.HandleAsync(new CancelScheduleCommand(postId), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.Equal(pubDate, post.PubDateUtc);
        Assert.NotNull(post.RouteLink);
    }

    [Fact]
    public async Task PostponePostCommand_ScheduledPost_AddsHoursToScheduledTime()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddHours(1);
        var post = CreatePostEntity(postId, PostStatus.Scheduled, "scheduled-post");
        post.ScheduledPublishTimeUtc = scheduledTime;
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PostponePostCommandHandler(db, Mock.Of<ILogger<PostponePostCommandHandler>>());

        await handler.HandleAsync(new PostponePostCommand(postId, 3), TestContext.Current.CancellationToken);

        var savedPost = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(scheduledTime.AddHours(3), savedPost.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task PostponePostCommand_DraftPost_DoesNotModifyPost()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Draft, "draft-post"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PostponePostCommandHandler(db, Mock.Of<ILogger<PostponePostCommandHandler>>());

        await handler.HandleAsync(new PostponePostCommand(postId, 3), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Null(post.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task EmptyRecycleBinCommand_DeletesOnlyDeletedPostsAndReturnsDeletedIds()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BlogDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var deletedPostId = Guid.NewGuid();
        var activePostId = Guid.NewGuid();
        var deletedPost = CreatePostEntity(deletedPostId, PostStatus.Published, "deleted-post", DateTime.UtcNow.AddDays(-2));
        deletedPost.IsDeleted = true;
        db.Post.Add(deletedPost);
        db.Post.Add(CreatePostEntity(activePostId, PostStatus.Published, "active-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new EmptyRecycleBinCommandHandler(db, Mock.Of<ILogger<EmptyRecycleBinCommandHandler>>());

        var result = await handler.HandleAsync(new EmptyRecycleBinCommand(), TestContext.Current.CancellationToken);

        Assert.Equal([deletedPostId], result);
        var remainingPost = await db.Post.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(activePostId, remainingPost.Id);
    }

    private static PostEntity CreatePostEntity(Guid id, PostStatus status, string slug, DateTime? pubDateUtc = null)
    {
        return new PostEntity
        {
            Id = id,
            Title = "Test Post",
            Slug = slug,
            Author = "Author",
            PostContent = "Content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-1),
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1),
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = pubDateUtc,
            PostStatus = status,
            IsDeleted = false,
            RouteLink = pubDateUtc.HasValue ? $"{pubDateUtc:yyyy/M/d}/{slug}" : null,
            ContentType = "html"
        };
    }
}
