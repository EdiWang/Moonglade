using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Features.Comment;
using Moq;

namespace Moonglade.Features.Tests;

public class CreateCommentCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_PostDoesNotExist_ReturnsNullAndDoesNotCreateComment()
    {
        using var db = CreateDbContext();
        var blogConfig = new BlogConfig
        {
            CommentSettings = new CommentSettings
            {
                CloseCommentAfterDays = 30,
                RequireCommentReview = false
            }
        };
        var logger = new Mock<ILogger<CreateCommentCommandHandler>>();
        var handler = new CreateCommentCommandHandler(blogConfig, logger.Object, db);
        var request = new CommentRequest
        {
            Username = "Reader",
            Content = "Nice post",
            Email = "reader@example.com",
            CaptchaCode = "1234",
            CaptchaToken = "token"
        };

        var result = await handler.HandleAsync(new CreateCommentCommand(Guid.NewGuid(), request, "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Empty(await db.Comment.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_NullPayload_ReturnsNullAndDoesNotCreateComment()
    {
        using var db = CreateDbContext();
        var postId = await SeedPostAsync(db, DateTime.UtcNow);
        var handler = CreateHandler(db, requireCommentReview: false, closeCommentAfterDays: 0);

        var result = await handler.HandleAsync(new CreateCommentCommand(postId, null, "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Empty(await db.Comment.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task HandleAsync_ValidPayload_CreatesCommentWithApprovalFromConfiguration(bool requireReview, bool expectedApproved)
    {
        using var db = CreateDbContext();
        var postId = await SeedPostAsync(db, DateTime.UtcNow);
        var handler = CreateHandler(db, requireReview, closeCommentAfterDays: 0);
        var request = CreateCommentRequest();

        var result = await handler.HandleAsync(new CreateCommentCommand(postId, request, "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.Content, result.CommentContent);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal("127.0.0.1", result.IpAddress);
        Assert.Equal("Test Post", result.PostTitle);
        Assert.Equal(expectedApproved, result.IsApproved);

        var comment = await db.Comment.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(expectedApproved, comment.IsApproved);
    }

    [Fact]
    public async Task HandleAsync_PostOlderThanCloseCommentAfterDays_ReturnsNullAndDoesNotCreateComment()
    {
        using var db = CreateDbContext();
        var postId = await SeedPostAsync(db, DateTime.UtcNow.AddDays(-10));
        var handler = CreateHandler(db, requireCommentReview: false, closeCommentAfterDays: 3);

        var result = await handler.HandleAsync(new CreateCommentCommand(postId, CreateCommentRequest(), "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Empty(await db.Comment.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HandleAsync_PostWithinCloseCommentAfterDays_CreatesComment()
    {
        using var db = CreateDbContext();
        var postId = await SeedPostAsync(db, DateTime.UtcNow.AddDays(-1));
        var handler = CreateHandler(db, requireCommentReview: false, closeCommentAfterDays: 3);

        var result = await handler.HandleAsync(new CreateCommentCommand(postId, CreateCommentRequest(), "127.0.0.1"), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(await db.Comment.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static CreateCommentCommandHandler CreateHandler(BlogDbContext db, bool requireCommentReview, int closeCommentAfterDays)
    {
        var blogConfig = new BlogConfig
        {
            CommentSettings = new CommentSettings
            {
                CloseCommentAfterDays = closeCommentAfterDays,
                RequireCommentReview = requireCommentReview
            }
        };
        var logger = new Mock<ILogger<CreateCommentCommandHandler>>();
        return new CreateCommentCommandHandler(blogConfig, logger.Object, db);
    }

    private static async Task<Guid> SeedPostAsync(BlogDbContext db, DateTime pubDateUtc)
    {
        var postId = Guid.NewGuid();
        db.Post.Add(new Data.Entities.PostEntity
        {
            Id = postId,
            Title = "Test Post",
            PubDateUtc = pubDateUtc
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return postId;
    }

    private static CommentRequest CreateCommentRequest()
    {
        return new CommentRequest
        {
            Username = "Reader",
            Content = "Nice post",
            Email = "reader@example.com",
            CaptchaCode = "1234",
            CaptchaToken = "token"
        };
    }
}

