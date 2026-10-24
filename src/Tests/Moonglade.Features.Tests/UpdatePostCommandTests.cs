using LiteBus.Events.Abstractions;
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
        var existingPost = CreatePostEntity(postId, PostStatus.Published, "old-slug", originalPubDate);
        existingPost.ScheduledPublishTimeUtc = DateTime.UtcNow.AddDays(1);
        db.Post.Add(existingPost);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var model = CreatePostEditModel(PostStatus.Published);
        model.Slug = "new-slug";

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(originalPubDate, post.PubDateUtc);
        Assert.Equal(originalPubDate, result.PubDateUtc);
        Assert.Equal("2024/1/2/new-slug", post.RouteLink);
        Assert.Null(post.ScheduledPublishTimeUtc);
    }

    [Theory]
    [InlineData(PostStatus.Draft)]
    [InlineData(PostStatus.Scheduled)]
    public async Task HandleAsync_UnpublishedToPublished_SetsPublishDateAndClearsSchedule(PostStatus previousStatus)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var existingPost = CreatePostEntity(postId, previousStatus, "unpublished-post");
        existingPost.ScheduledPublishTimeUtc = previousStatus == PostStatus.Scheduled ? DateTime.UtcNow.AddDays(1) : null;
        db.Post.Add(existingPost);
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
        Assert.Equal(result.RouteLink, post.RouteLink);
        Assert.Null(post.ScheduledPublishTimeUtc);
    }

    [Theory]
    [InlineData(PostStatus.Published)]
    [InlineData(PostStatus.Scheduled)]
    public async Task HandleAsync_ToDraft_ClearsPublishScheduleAndRouteLink(PostStatus previousStatus)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var existingPost = CreatePostEntity(postId, previousStatus, "test-post",
            previousStatus == PostStatus.Published ? DateTime.UtcNow.AddDays(-1) : null);
        existingPost.ScheduledPublishTimeUtc = previousStatus == PostStatus.Scheduled ? DateTime.UtcNow.AddDays(1) : null;
        db.Post.Add(existingPost);
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

    [Theory]
    [InlineData(PostStatus.Draft)]
    [InlineData(PostStatus.Scheduled)]
    [InlineData(PostStatus.Published)]
    public async Task HandleAsync_ToScheduled_SetsScheduledTimeAndClearsPublishedRoute(PostStatus previousStatus)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var existingPost = CreatePostEntity(postId, previousStatus, "test-post",
            previousStatus == PostStatus.Published ? DateTime.UtcNow.AddDays(-1) : null);
        existingPost.ScheduledPublishTimeUtc = previousStatus == PostStatus.Scheduled ? DateTime.UtcNow.AddHours(1) : null;
        db.Post.Add(existingPost);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var model = CreatePostEditModel(PostStatus.Scheduled);
        model.ScheduledPublishTimeUtc = scheduledTime;
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
    public async Task HandleAsync_WithAiAssistedContent_UpdatesDisclosure()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId, PostStatus.Draft, "draft-post"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var model = CreatePostEditModel(PostStatus.Draft);
        model.ContainsAiAssistedContent = true;
        var handler = CreateHandler(db);

        await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.True(post.ContainsAiAssistedContent);
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

    [Theory]
    [InlineData(PostStatus.Default)]
    [InlineData(PostStatus.Deleted)]
    [InlineData((PostStatus)99)]
    public async Task HandleAsync_InvalidWriteStatus_DoesNotPersistChanges(PostStatus status)
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var pubDateUtc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var post = CreatePostEntity(postId, PostStatus.Published, "published-post", pubDateUtc);
        db.Post.Add(post);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var handler = CreateHandler(db);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new UpdatePostCommand(postId, CreatePostEditModel(status)), TestContext.Current.CancellationToken));

        await db.Entry(post).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.Equal(pubDateUtc, post.PubDateUtc);
        Assert.Equal("published-post", post.Slug);
    }

    private static UpdatePostCommandHandler CreateHandler(BlogDbContext db)
    {
        var logger = new Mock<ILogger<UpdatePostCommandHandler>>();
        return new UpdatePostCommandHandler(db, logger.Object, Mock.Of<IEventMediator>());
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
