using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;

namespace Moonglade.Features.Tests;

public class PublishScheduledPostCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_DueScheduledPost_PublishesPostAndGeneratesRouteLink()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Scheduled, "scheduled-post", DateTime.UtcNow.AddMinutes(-5)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PublishScheduledPostCommandHandler(db);
        var before = DateTime.UtcNow;

        var result = await handler.HandleAsync(new PublishScheduledPostCommand(), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.True(result > 0);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.NotNull(post.PubDateUtc);
        Assert.InRange(post.PubDateUtc.Value, before, after);
        Assert.Null(post.ScheduledPublishTimeUtc);
        Assert.NotNull(post.RouteLink);
        Assert.Contains("scheduled-post", post.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_FutureScheduledPost_DoesNotPublishPost()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddHours(1);
        db.Post.Add(CreatePostEntity(postId, PostStatus.Scheduled, "future-post", scheduledTime));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PublishScheduledPostCommandHandler(db);

        var result = await handler.HandleAsync(new PublishScheduledPostCommand(), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(0, result);
        Assert.Equal(PostStatus.Scheduled, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Equal(scheduledTime, post.ScheduledPublishTimeUtc);
        Assert.Null(post.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_DeletedScheduledPost_DoesNotPublishPost()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddMinutes(-5);
        var post = CreatePostEntity(postId, PostStatus.Scheduled, "deleted-scheduled-post", scheduledTime);
        post.IsDeleted = true;
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PublishScheduledPostCommandHandler(db);

        var result = await handler.HandleAsync(new PublishScheduledPostCommand(), TestContext.Current.CancellationToken);

        var savedPost = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(0, result);
        Assert.Equal(PostStatus.Scheduled, savedPost.PostStatus);
        Assert.True(savedPost.IsDeleted);
        Assert.Null(savedPost.PubDateUtc);
        Assert.Equal(scheduledTime, savedPost.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task HandleAsync_NoDueScheduledPosts_ReturnsZero()
    {
        using var db = CreateDbContext();
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Draft, "draft-post"));
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Published, "published-post", pubDateUtc: DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new PublishScheduledPostCommandHandler(db);

        var result = await handler.HandleAsync(new PublishScheduledPostCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(0, result);
    }

    private static PostEntity CreatePostEntity(Guid id, PostStatus status, string slug, DateTime? scheduledPublishTimeUtc = null, DateTime? pubDateUtc = null)
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
            ScheduledPublishTimeUtc = scheduledPublishTimeUtc,
            PostStatus = status,
            IsDeleted = false,
            RouteLink = pubDateUtc.HasValue ? $"{pubDateUtc:yyyy/M/d}/{slug}" : null,
            ContentType = "html"
        };
    }
}
