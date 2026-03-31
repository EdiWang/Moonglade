using Moonglade.Configuration;
using Moq;

namespace Moonglade.Email.Client.Tests;

public class CommentNotificationEventHandlerTests
{
    private readonly Mock<IMoongladeEmailClient> _emailClientMock;
    private readonly Mock<IBlogConfig> _blogConfigMock;
    private readonly CommentNotificationEventHandler _handler;

    public CommentNotificationEventHandlerTests()
    {
        _emailClientMock = new Mock<IMoongladeEmailClient>();
        _blogConfigMock = new Mock<IBlogConfig>();

        _blogConfigMock.Setup(c => c.GeneralSettings).Returns(new GeneralSettings
        {
            OwnerEmail = "owner@blog.com"
        });

        _handler = new CommentNotificationEventHandler(_emailClientMock.Object, _blogConfigMock.Object);
    }

    [Fact]
    public async Task HandleAsync_SendsToOwnerEmail()
    {
        var evt = new CommentEvent("user1", "user@test.com", "127.0.0.1", "My Post", "Hello **world**");

        await _handler.HandleAsync(evt, CancellationToken.None);

        _emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.NewCommentNotification,
                It.Is<string[]>(r => r.Length == 1 && r[0] == "owner@blog.com"),
                It.IsAny<CommentEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ConvertsMarkdownCommentContent()
    {
        var evt = new CommentEvent("user1", "user@test.com", "127.0.0.1", "My Post", "**bold**");

        CommentEvent capturedEvent = null;
        _emailClientMock
            .Setup(c => c.SendEmailAsync(
                It.IsAny<MailMesageTypes>(),
                It.IsAny<string[]>(),
                It.IsAny<CommentEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<MailMesageTypes, string[], CommentEvent, CancellationToken>((_, _, e, _) => capturedEvent = e)
            .ReturnsAsync(true);

        await _handler.HandleAsync(evt, CancellationToken.None);

        Assert.NotNull(capturedEvent);
        Assert.Contains("<strong>bold</strong>", capturedEvent.CommentContent);
    }

    [Fact]
    public async Task HandleAsync_UsesCorrectMessageType()
    {
        var evt = new CommentEvent("user1", "user@test.com", "127.0.0.1", "Post", "Content");

        await _handler.HandleAsync(evt, CancellationToken.None);

        _emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.NewCommentNotification,
                It.IsAny<string[]>(),
                It.IsAny<CommentEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
