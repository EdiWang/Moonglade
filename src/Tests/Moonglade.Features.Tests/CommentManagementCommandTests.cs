using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Features.Comment;
using Moq;

namespace Moonglade.Features.Tests;

public class CommentManagementCommandTests
{
    private static BlogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BlogDbContext(options);
    }

    [Fact]
    public async Task ReplyCommentCommand_CreatesReplyAndReturnsCommentReply()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId));
        db.Comment.Add(CreateCommentEntity(commentId, postId, isApproved: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ReplyCommentCommandHandler(Mock.Of<ILogger<ReplyCommentCommandHandler>>(), db);

        var result = await handler.HandleAsync(new ReplyCommentCommand(commentId, "**Thanks**"), TestContext.Current.CancellationToken);

        Assert.Equal(commentId, result.CommentId);
        Assert.Equal(postId, result.PostId);
        Assert.Equal("**Thanks**", result.ReplyContent);
        Assert.Contains("<strong>Thanks</strong>", result.ReplyContentHtml);
        Assert.Equal("Test Post", result.Title);
        Assert.Equal("2024/1/1/test-post", result.RouteLink);
        Assert.Equal("reader@example.com", result.Email);
        Assert.Equal("Nice post", result.CommentContent);

        var reply = await db.CommentReply.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(commentId, reply.CommentId);
        Assert.Equal("**Thanks**", reply.ReplyContent);
    }

    [Fact]
    public async Task ReplyCommentCommand_CommentDoesNotExist_ThrowsInvalidOperationException()
    {
        using var db = CreateDbContext();
        var commentId = Guid.NewGuid();
        var handler = new ReplyCommentCommandHandler(Mock.Of<ILogger<ReplyCommentCommandHandler>>(), db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ReplyCommentCommand(commentId, "Reply"), TestContext.Current.CancellationToken));

        Assert.Contains(commentId.ToString(), exception.Message);
    }

    [Fact]
    public async Task ToggleApprovalCommand_TogglesMatchingComments()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var untouchedId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId));
        db.Comment.AddRange(
            CreateCommentEntity(firstId, postId, isApproved: true),
            CreateCommentEntity(secondId, postId, isApproved: false),
            CreateCommentEntity(untouchedId, postId, isApproved: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ToggleApprovalCommandHandler(db, Mock.Of<ILogger<ToggleApprovalCommandHandler>>());

        await handler.HandleAsync(new ToggleApprovalCommand([firstId, secondId]), TestContext.Current.CancellationToken);

        Assert.False((await db.Comment.FindAsync([firstId], TestContext.Current.CancellationToken))!.IsApproved);
        Assert.True((await db.Comment.FindAsync([secondId], TestContext.Current.CancellationToken))!.IsApproved);
        Assert.True((await db.Comment.FindAsync([untouchedId], TestContext.Current.CancellationToken))!.IsApproved);
    }

    [Fact]
    public async Task ToggleApprovalCommand_EmptyIds_DoesNotModifyComments()
    {
        using var db = CreateDbContext();
        var postId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        db.Post.Add(CreatePostEntity(postId));
        db.Comment.Add(CreateCommentEntity(commentId, postId, isApproved: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ToggleApprovalCommandHandler(db, Mock.Of<ILogger<ToggleApprovalCommandHandler>>());

        await handler.HandleAsync(new ToggleApprovalCommand([]), TestContext.Current.CancellationToken);

        Assert.True((await db.Comment.FindAsync([commentId], TestContext.Current.CancellationToken))!.IsApproved);
    }

    [Fact]
    public async Task DeleteCommentsCommand_RemovesMatchingCommentsAndDetachesReplies()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<BlogDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new BlogDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var postId = Guid.NewGuid();
        var deleteId = Guid.NewGuid();
        var keepId = Guid.NewGuid();
        var deleteComment = CreateCommentEntity(deleteId, postId, isApproved: true);
        deleteComment.Replies.Add(new CommentReplyEntity
        {
            Id = Guid.NewGuid(),
            CommentId = deleteId,
            ReplyContent = "Reply",
            CreateTimeUtc = DateTime.UtcNow
        });
        db.Post.Add(CreatePostEntity(postId));
        db.Comment.AddRange(deleteComment, CreateCommentEntity(keepId, postId, isApproved: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteCommentsCommandHandler(db, Mock.Of<ILogger<DeleteCommentsCommandHandler>>());

        await handler.HandleAsync(new DeleteCommentsCommand([deleteId]), TestContext.Current.CancellationToken);

        Assert.Null(await db.Comment.FindAsync([deleteId], TestContext.Current.CancellationToken));
        Assert.NotNull(await db.Comment.FindAsync([keepId], TestContext.Current.CancellationToken));
        var detachedReply = await db.CommentReply.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(detachedReply.CommentId);
    }

    private static PostEntity CreatePostEntity(Guid id)
    {
        return new PostEntity
        {
            Id = id,
            Title = "Test Post",
            Slug = "test-post",
            RouteLink = "2024/1/1/test-post",
            PostStatus = PostStatus.Published,
            PubDateUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            PostContent = "Content",
            ContentAbstract = "Abstract",
            ContentLanguageCode = "en-us",
            ContentType = "html"
        };
    }

    private static CommentEntity CreateCommentEntity(Guid id, Guid postId, bool isApproved)
    {
        return new CommentEntity
        {
            Id = id,
            PostId = postId,
            Username = "Reader",
            Email = "reader@example.com",
            IPAddress = "127.0.0.1",
            CommentContent = "Nice post",
            CreateTimeUtc = DateTime.UtcNow,
            IsApproved = isApproved
        };
    }
}
