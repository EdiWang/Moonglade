using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;

namespace Moonglade.Features.Tests;

public class PostQueryTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task ListPostsQuery_ReturnsOnlyPublishedNotDeletedPostsOrderedByPublishDate()
    {
        using var db = CreateDbContext();
        db.Post.AddRange(
            CreatePost(Guid.NewGuid(), "old", PostStatus.Published, DateTime.UtcNow.AddDays(-2)),
            CreatePost(Guid.NewGuid(), "new", PostStatus.Published, DateTime.UtcNow.AddDays(-1)),
            CreatePost(Guid.NewGuid(), "draft", PostStatus.Draft, null),
            CreatePost(Guid.NewGuid(), "deleted", PostStatus.Published, DateTime.UtcNow, isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListPostsQueryHandler(db);

        var result = await handler.HandleAsync(new ListPostsQuery(10, 1), TestContext.Current.CancellationToken);

        Assert.Collection(result,
            p => Assert.Equal("new", p.Slug),
            p => Assert.Equal("old", p.Slug));
    }

    [Fact]
    public async Task ListPostsQuery_FiltersByCategory()
    {
        using var db = CreateDbContext();
        var targetCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var targetPost = CreatePost(Guid.NewGuid(), "target", PostStatus.Published, DateTime.UtcNow.AddDays(-1));
        targetPost.PostCategory.Add(new PostCategoryEntity { PostId = targetPost.Id, CategoryId = targetCategoryId });
        var otherPost = CreatePost(Guid.NewGuid(), "other", PostStatus.Published, DateTime.UtcNow);
        otherPost.PostCategory.Add(new PostCategoryEntity { PostId = otherPost.Id, CategoryId = otherCategoryId });
        db.Post.AddRange(targetPost, otherPost);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListPostsQueryHandler(db);

        var result = await handler.HandleAsync(new ListPostsQuery(10, 1, targetCategoryId), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("target", result[0].Slug);
    }

    [Fact]
    public async Task SearchPostQuery_EmptyKeyword_ThrowsArgumentException()
    {
        using var db = CreateDbContext();
        var handler = new SearchPostQueryHandler(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(new SearchPostQuery("   "), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SearchPostQuery_SingleWordSearchesTitleOrTag()
    {
        using var db = CreateDbContext();
        var titlePost = CreatePost(Guid.NewGuid(), "title-match", PostStatus.Published, DateTime.UtcNow.AddDays(-1), title: "Azure Functions Guide");
        var tagPost = CreatePost(Guid.NewGuid(), "tag-match", PostStatus.Published, DateTime.UtcNow.AddDays(-2), title: "Serverless Notes");
        tagPost.Tags.Add(new TagEntity { DisplayName = "Azure", NormalizedName = "azure" });
        db.Post.AddRange(titlePost, tagPost, CreatePost(Guid.NewGuid(), "draft", PostStatus.Draft, null, title: "Azure Draft"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchPostQueryHandler(db);

        var result = await handler.HandleAsync(new SearchPostQuery("Azure"), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Slug == "title-match");
        Assert.Contains(result, p => p.Slug == "tag-match");
        Assert.DoesNotContain(result, p => p.Slug == "draft");
    }

    [Fact]
    public async Task SearchPostQuery_MultipleWordsRequiresAllWordsInTitle()
    {
        using var db = CreateDbContext();
        db.Post.AddRange(
            CreatePost(Guid.NewGuid(), "match", PostStatus.Published, DateTime.UtcNow, title: "Azure Functions Guide"),
            CreatePost(Guid.NewGuid(), "partial", PostStatus.Published, DateTime.UtcNow.AddDays(-1), title: "Azure Guide"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new SearchPostQueryHandler(db);

        var result = await handler.HandleAsync(new SearchPostQuery("Azure Functions"), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("match", result[0].Slug);
    }

    [Fact]
    public async Task GetPostBySlugQuery_ReturnsOnlyPublishedNotDeletedPost()
    {
        using var db = CreateDbContext();
        db.Post.AddRange(
            CreatePost(Guid.NewGuid(), "published", PostStatus.Published, DateTime.UtcNow, routeLink: "2024/1/1/published"),
            CreatePost(Guid.NewGuid(), "draft", PostStatus.Draft, null, routeLink: "2024/1/1/draft"),
            CreatePost(Guid.NewGuid(), "deleted", PostStatus.Published, DateTime.UtcNow, isDeleted: true, routeLink: "2024/1/1/deleted"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPostBySlugQueryHandler(db);

        var found = await handler.HandleAsync(new GetPostBySlugQuery("2024/1/1/published"), TestContext.Current.CancellationToken);
        var draft = await handler.HandleAsync(new GetPostBySlugQuery("2024/1/1/draft"), TestContext.Current.CancellationToken);
        var deleted = await handler.HandleAsync(new GetPostBySlugQuery("2024/1/1/deleted"), TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("published", found.Slug);
        Assert.Null(draft);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ListFeaturedQuery_ReturnsOnlyFeaturedPublishedPosts()
    {
        using var db = CreateDbContext();
        db.Post.AddRange(
            CreatePost(Guid.NewGuid(), "featured", PostStatus.Published, DateTime.UtcNow, isFeatured: true),
            CreatePost(Guid.NewGuid(), "not-featured", PostStatus.Published, DateTime.UtcNow.AddDays(-1)),
            CreatePost(Guid.NewGuid(), "draft-featured", PostStatus.Draft, null, isFeatured: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListFeaturedQueryHandler(db);

        var result = await handler.HandleAsync(new ListFeaturedQuery(10, 1), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("featured", result[0].Slug);
    }

    [Fact]
    public async Task ListByTagQuery_ReturnsPublishedPostsForTag()
    {
        using var db = CreateDbContext();
        var tag = new TagEntity { DisplayName = "Azure", NormalizedName = "azure" };
        var taggedPost = CreatePost(Guid.NewGuid(), "tagged", PostStatus.Published, DateTime.UtcNow);
        taggedPost.Tags.Add(tag);
        var draftTaggedPost = CreatePost(Guid.NewGuid(), "draft-tagged", PostStatus.Draft, null);
        draftTaggedPost.Tags.Add(tag);
        db.Post.AddRange(taggedPost, draftTaggedPost);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ListByTagQueryHandler(db);

        var result = await handler.HandleAsync(new ListByTagQuery(tag.Id, 10, 1), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal("tagged", result[0].Slug);
    }

    [Fact]
    public async Task GetArchiveQuery_GroupsPublishedPostsByYearAndMonth()
    {
        using var db = CreateDbContext();
        db.Post.AddRange(
            CreatePost(Guid.NewGuid(), "jan-1", PostStatus.Published, new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost(Guid.NewGuid(), "jan-2", PostStatus.Published, new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost(Guid.NewGuid(), "feb", PostStatus.Published, new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost(Guid.NewGuid(), "draft", PostStatus.Draft, null));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetArchiveQueryHandler(db);

        var result = await handler.HandleAsync(new GetArchiveQuery(), TestContext.Current.CancellationToken);

        Assert.Contains(result, a => a.Year == 2024 && a.Month == 1 && a.Count == 2);
        Assert.Contains(result, a => a.Year == 2024 && a.Month == 2 && a.Count == 1);
        Assert.Equal(2, result.Count);
    }

    private static PostEntity CreatePost(
        Guid id,
        string slug,
        PostStatus status,
        DateTime? pubDateUtc,
        bool isDeleted = false,
        bool isFeatured = false,
        string? title = null,
        string? routeLink = null)
    {
        return new PostEntity
        {
            Id = id,
            Title = title ?? slug,
            Slug = slug,
            Author = "Author",
            PostContent = "Content",
            CommentEnabled = true,
            CreateTimeUtc = DateTime.UtcNow.AddDays(-3),
            LastModifiedUtc = DateTime.UtcNow.AddDays(-2),
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            IsFeedIncluded = true,
            PubDateUtc = pubDateUtc,
            PostStatus = status,
            IsDeleted = isDeleted,
            IsFeatured = isFeatured,
            RouteLink = routeLink ?? (pubDateUtc.HasValue ? $"{pubDateUtc:yyyy/M/d}/{slug}" : null),
            ContentType = "html"
        };
    }
}
