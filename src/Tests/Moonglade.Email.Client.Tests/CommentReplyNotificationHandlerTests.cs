using Moq;

namespace Moonglade.Email.Client.Tests;

public class CommentReplyNotificationHandlerTests
{
    private readonly Mock<IMoongladeEmailClient> _emailClientMock;
    private readonly CommentReplyNotificationHandler _handler;

    public CommentReplyNotificationHandlerTests()
    {
        _emailClientMock = new Mock<IMoongladeEmailClient>();
        _handler = new CommentReplyNotificationHandler(_emailClientMock.Object);
    }

    [Fact]
    public async Task HandleAsync_SendsToCommenterEmail()
    {
        var evt = new CommentReplyEvent(
            "commenter@test.com",
            "Original comment",
            "Post Title",
            "<p>Thank you</p>",
            "https://blog.com/post/test");

        await _handler.HandleAsync(evt, CancellationToken.None);

        _emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.AdminReplyNotification,
                It.Is<string[]>(r => r.Length == 1 && r[0] == "commenter@test.com"),
                It.IsAny<CommentReplyEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UsesAdminReplyMessageType()
    {
        var evt = new CommentReplyEvent("a@b.com", "comment", "Title", "<p>Reply</p>", "https://blog.com/post/1");

        await _handler.HandleAsync(evt, CancellationToken.None);

        _emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.AdminReplyNotification,
                It.IsAny<string[]>(),
                It.IsAny<CommentReplyEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PassesEventAsPayload()
    {
        var evt = new CommentReplyEvent("a@b.com", "comment", "Title", "<p>Reply</p>", "https://blog.com/post/1");

        CommentReplyEvent captured = null;
        _emailClientMock
            .Setup(c => c.SendEmailAsync(
                It.IsAny<MailMesageTypes>(),
                It.IsAny<string[]>(),
                It.IsAny<CommentReplyEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<MailMesageTypes, string[], CommentReplyEvent, CancellationToken>((_, _, e, _) => captured = e)
            .ReturnsAsync(true);

        await _handler.HandleAsync(evt, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Title", captured.Title);
        Assert.Equal("<p>Reply</p>", captured.ReplyContentHtml);
        Assert.Equal("https://blog.com/post/1", captured.PostLink);
    }
}
