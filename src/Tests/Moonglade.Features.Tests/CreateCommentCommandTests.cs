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
}

