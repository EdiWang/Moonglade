using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests;

public class UpdatePostCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ExistingPublishedPost_DoesNotResetPublishDate()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var originalPubDate = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "old-slug", originalPubDate));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var model = CreatePostEditModel(PostStatus.Published);
        model.Slug = "new-slug";

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(originalPubDate, post.PubDateUtc);
        Assert.Equal(originalPubDate, result.PubDateUtc);
        Assert.Contains("new-slug", post.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_DraftToPublished_SetsPublishDateAndRouteLink()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Draft, "draft-post"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var model = CreatePostEditModel(PostStatus.Published);
        var before = DateTime.UtcNow;

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.NotNull(post.PubDateUtc);
        Assert.InRange(post.PubDateUtc.Value, before, after);
        Assert.NotNull(result.RouteLink);
        Assert.Contains("test-post", result.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_ToDraft_ClearsPublishScheduleAndRouteLink()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, CreatePostEditModel(PostStatus.Draft)), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Draft, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.ScheduledPublishTimeUtc);
        Assert.Null(post.RouteLink);
        Assert.Null(result.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_ToScheduled_SetsScheduledTimeAndClearsPublishedRoute()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Published, "published-post", DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var model = CreatePostEditModel(PostStatus.Scheduled);
        model.ScheduledPublishTime = scheduledTime;
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Scheduled, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Equal(scheduledTime, post.ScheduledPublishTimeUtc);
        Assert.Null(post.RouteLink);
        Assert.Null(result.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_ReplacesCategoriesAndTags()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var oldCategoryId = Guid.NewGuid();
        var newCategoryId = Guid.NewGuid();
        var oldTag = new TagEntity { DisplayName = "old", NormalizedName = "old" };
        var post = CreatePostEntity(postId, PostStatus.Draft, "draft-post");
        post.PostCategory.Add(new PostCategoryEntity { PostId = postId, CategoryId = oldCategoryId });
        post.Tags.Add(oldTag);

        db.Category.AddRange(
            new CategoryEntity { Id = oldCategoryId, Slug = "old", DisplayName = "Old", Note = "Old" },
            new CategoryEntity { Id = newCategoryId, Slug = "new", DisplayName = "New", Note = "New" });
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var model = CreatePostEditModel(PostStatus.Draft);
        model.SelectedCatIds = [newCategoryId];
        model.Tags = "new-tag";
        var handler = CreateHandler(db);

        await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var updatedPost = await db.Post
            .Include(p => p.PostCategory)
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Collection(updatedPost.PostCategory, pc => Assert.Equal(newCategoryId, pc.CategoryId));
        Assert.DoesNotContain(updatedPost.Tags, t => t.DisplayName == "old");
        Assert.Contains(updatedPost.Tags, t => t.DisplayName == "new-tag");
    }

    [Fact]
    public async Task HandleAsync_PostDoesNotExist_ThrowsInvalidOperationException()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var postId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new UpdatePostCommand(postId, CreatePostEditModel(PostStatus.Draft)), TestContext.Current.CancellationToken));

        Assert.Contains(postId.ToString(), exception.Message);
    }

    private static UpdatePostCommandHandler CreateHandler(BlogDbContext db)
    {
        var logger = new Mock<ILogger<UpdatePostCommandHandler>>();
        return new UpdatePostCommandHandler(db, logger.Object);
    }

    private static PostEditModel CreatePostEditModel(PostStatus postStatus)
    {
        return new PostEditModel
        {
            Title = "Test Post",
            Slug = "test-post",
            Author = "Author",
            SelectedCatIds = [],
            EnableComment = true,
            EditorContent = "Hello world",
            PostStatus = postStatus,
            ContentType = "html",
            Featured = false,
            FeedIncluded = true,
            Tags = string.Empty,
            LanguageCode = "en-us",
            Abstract = "Abstract",
            Keywords = string.Empty
        };
    }

    private static PostEntity CreatePostEntity(Guid id, PostStatus status, string slug, DateTime? pubDateUtc = null)
    {
        return new PostEntity
        {
            Id = id,
            Title = "Existing Post",
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
