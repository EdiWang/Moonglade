using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests;

public class CreatePostCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_DraftPost_DoesNotSetPublishDateOrRouteLink()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new CreatePostCommand(CreatePostEditModel(PostStatus.Draft)), TestContext.Current.CancellationToken);

        Assert.Null(result.PubDateUtc);
        Assert.Null(result.RouteLink);

        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Draft, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.RouteLink);
        Assert.Null(post.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task HandleAsync_PublishedPost_SetsPublishDateAndRouteLink()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var before = DateTime.UtcNow;

        var result = await handler.HandleAsync(new CreatePostCommand(CreatePostEditModel(PostStatus.Published)), TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        Assert.NotNull(result.PubDateUtc);
        Assert.InRange(result.PubDateUtc.Value, before, after);
        Assert.NotNull(result.RouteLink);
        Assert.Contains("test-post", result.RouteLink);

        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Published, post.PostStatus);
        Assert.Equal(result.PubDateUtc, post.PubDateUtc);
        Assert.Equal(result.RouteLink, post.RouteLink);
        Assert.Null(post.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task HandleAsync_ScheduledPost_SetsScheduledTimeAndDoesNotGenerateRouteLink()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var scheduledTime = DateTime.UtcNow.AddHours(2);
        var model = CreatePostEditModel(PostStatus.Scheduled);
        model.ScheduledPublishTime = scheduledTime;

        var result = await handler.HandleAsync(new CreatePostCommand(model), TestContext.Current.CancellationToken);

        Assert.Null(result.PubDateUtc);
        Assert.Null(result.RouteLink);

        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Scheduled, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.RouteLink);
        Assert.Equal(scheduledTime, post.ScheduledPublishTimeUtc);
    }

    [Fact]
    public async Task HandleAsync_NormalizesTextFields()
    {
        using var db = CreateDbContext();
        var handler = CreateHandler(db);
        var model = CreatePostEditModel(PostStatus.Draft);
        model.Title = "  Test Title  ";
        model.Slug = "  Mixed-Case-Slug  ";
        model.Author = "  Author Name  ";
        model.Abstract = "  Abstract Text  ";

        var result = await handler.HandleAsync(new CreatePostCommand(model), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Equal("Test Title", post.Title);
        Assert.Equal("mixed-case-slug", post.Slug);
        Assert.Equal("Author Name", post.Author);
        Assert.Equal("Abstract Text", post.ContentAbstract);
    }

    [Fact]
    public async Task HandleAsync_PublishedSlugConflict_AppendsSuffixToSlug()
    {
        using var db = CreateDbContext();
        var existingPublishDate = DateTime.UtcNow;
        db.Post.Add(CreatePostEntity(Guid.NewGuid(), PostStatus.Published, "test-post", existingPublishDate));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);

        var result = await handler.HandleAsync(new CreatePostCommand(CreatePostEditModel(PostStatus.Published)), TestContext.Current.CancellationToken);

        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.StartsWith("test-post-", post.Slug);
        Assert.NotEqual("test-post", post.Slug);
        Assert.Contains(post.Slug, result.RouteLink);
    }

    [Fact]
    public async Task HandleAsync_AssignsCategoriesAndCreatesTags()
    {
        using var db = CreateDbContext();
        var categoryId = Guid.NewGuid();
        db.Category.Add(new CategoryEntity
        {
            Id = categoryId,
            Slug = "dotnet",
            DisplayName = ".NET",
            Note = ".NET posts"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = CreateHandler(db);
        var model = CreatePostEditModel(PostStatus.Draft);
        model.SelectedCatIds = [categoryId];
        model.Tags = "dotnet,aspnetcore";

        var result = await handler.HandleAsync(new CreatePostCommand(model), TestContext.Current.CancellationToken);

        var post = await db.Post
            .Include(p => p.PostCategory)
            .Include(p => p.Tags)
            .SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);

        Assert.Collection(post.PostCategory, pc => Assert.Equal(categoryId, pc.CategoryId));
        Assert.Equal(2, post.Tags.Count);
        Assert.Contains(post.Tags, t => t.DisplayName == "dotnet");
        Assert.Contains(post.Tags, t => t.DisplayName == "aspnetcore");
    }

    private static CreatePostCommandHandler CreateHandler(BlogDbContext db)
    {
        var logger = new Mock<ILogger<CreatePostCommandHandler>>();
        return new CreatePostCommandHandler(db, logger.Object);
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
