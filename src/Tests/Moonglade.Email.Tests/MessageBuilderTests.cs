using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Moonglade.Email.Core;
using Moq;

namespace Moonglade.Email.Tests;

public class MessageBuilderTests
{
    private readonly Mock<IEmailHelper> _mockEmailHelper;
    private readonly MessageBuilder _sut;

    public MessageBuilderTests()
    {
        _mockEmailHelper = new Mock<IEmailHelper>();
        _mockEmailHelper
            .Setup(h => h.ForType(It.IsAny<string>()))
            .Returns(_mockEmailHelper.Object);
        _mockEmailHelper
            .Setup(h => h.Map(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(_mockEmailHelper.Object);

        _sut = new MessageBuilder(_mockEmailHelper.Object);
    }

    [Fact]
    public void BuildTestNotification_CallsForTypeWithTestMail()
    {
        var toAddresses = new[] { "test@example.com" };
        var smtpSettings = new EmailSettings
        {
            SmtpSettings = new SmtpSettings("localhost", string.Empty, string.Empty, 25)
        };
        var expectedMessage = new CommonMailMessage { Subject = "Test" };

        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(expectedMessage);

        var result = _sut.BuildTestNotification(toAddresses, smtpSettings);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.TestMail), Times.Once);
        Assert.Equal(expectedMessage, result);
    }

    [Fact]
    public void BuildNewCommentNotification_MapsAllPayloadFields()
    {
        var toAddresses = new[] { "admin@example.com" };
        var payload = new NewCommentPayload
        {
            Username = "JohnDoe",
            Email = "john@example.com",
            IpAddress = "127.0.0.1",
            PostTitle = "Hello World",
            CommentContent = "Great post"
        };

        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildNewCommentNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.NewCommentNotification), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.Username), payload.Username), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.Email), payload.Email), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.IpAddress), payload.IpAddress), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.PostTitle), payload.PostTitle), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.CommentContent), payload.CommentContent), Times.Once);
    }

    [Fact]
    public void BuildCommentReplyNotification_MapsAllPayloadFields()
    {
        var toAddresses = new[] { "user@example.com" };
        var payload = new CommentReplyPayload
        {
            Email = "user@example.com",
            CommentContent = "Original comment",
            Title = "My Post",
            ReplyContentHtml = "<p>Thanks</p>",
            PostLink = "https://example.com/posts/1"
        };

        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildCommentReplyNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.AdminReplyNotification), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.ReplyContentHtml), payload.ReplyContentHtml), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.PostLink), payload.PostLink), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.Title), payload.Title), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.CommentContent), payload.CommentContent), Times.Once);
    }

    [Fact]
    public void BuildPingNotification_MapsAllPayloadFields()
    {
        var toAddresses = new[] { "admin@example.com" };
        var payload = new PingPayload
        {
            TargetPostTitle = "My Post",
            Domain = "pingback.example.com",
            SourceIp = "1.2.3.4",
            SourceUrl = "https://pingback.example.com/post",
            SourceTitle = "Another Blog"
        };

        _mockEmailHelper
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildPingNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.BeingPinged), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.TargetPostTitle), payload.TargetPostTitle), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.Domain), payload.Domain), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.SourceIp), payload.SourceIp), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.SourceUrl), payload.SourceUrl), Times.Once);
        _mockEmailHelper.Verify(h => h.Map(nameof(payload.SourceTitle), payload.SourceTitle), Times.Once);
    }
}
