using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.Features.Tests;

public class PostRouteLinkTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task CreatePost_Draft_DoesNotGenerateRouteLink()
    {
        using var db = CreateDbContext();
        var logger = new Mock<ILogger<CreatePostCommandHandler>>();
        var handler = new CreatePostCommandHandler(db, logger.Object);

        var result = await handler.HandleAsync(new CreatePostCommand(CreatePostEditModel(PostStatus.Draft)), TestContext.Current.CancellationToken);

        Assert.Null(result.RouteLink);
        var post = await db.Post.SingleAsync(p => p.Id == result.Id, TestContext.Current.CancellationToken);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.RouteLink);
    }

    [Fact]
    public async Task CreatePost_Published_GeneratesRouteLinkFromPublishDate()
    {
        using var db = CreateDbContext();
        var logger = new Mock<ILogger<CreatePostCommandHandler>>();
        var handler = new CreatePostCommandHandler(db, logger.Object);

        var result = await handler.HandleAsync(new CreatePostCommand(CreatePostEditModel(PostStatus.Published)), TestContext.Current.CancellationToken);

        Assert.NotNull(result.RouteLink);
        Assert.Contains("test-post", result.RouteLink);
        Assert.DoesNotContain("0001/1/1", result.RouteLink);
    }

    [Fact]
    public async Task UpdatePost_ToDraft_ClearsPublishDateAndRouteLink()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();

        db.Post.Add(new PostEntity
        {
            Id = postId,
            Title = "Published Post",
            Slug = "published-post",
            Author = "Author",
            PostContent = "Content",
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            ContentType = "html",
            CreateTimeUtc = DateTime.UtcNow.AddDays(-1),
            LastModifiedUtc = DateTime.UtcNow.AddDays(-1),
            PubDateUtc = DateTime.UtcNow.AddDays(-1),
            RouteLink = "2026/4/26/published-post",
            PostStatus = PostStatus.Published
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var logger = new Mock<ILogger<UpdatePostCommandHandler>>();
        var handler = new UpdatePostCommandHandler(db, logger.Object);
        var model = CreatePostEditModel(PostStatus.Draft);
        model.PostId = postId;

        var result = await handler.HandleAsync(new UpdatePostCommand(postId, model), TestContext.Current.CancellationToken);

        Assert.Null(result.RouteLink);
        var post = await db.Post.SingleAsync(p => p.Id == postId, TestContext.Current.CancellationToken);
        Assert.Equal(PostStatus.Draft, post.PostStatus);
        Assert.Null(post.PubDateUtc);
        Assert.Null(post.RouteLink);
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
}

