using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Security;
using Moonglade.Email.Core;
using Moq;

namespace Moonglade.Email.Tests.Notifications;

public class MessageBuilderTests
{
    private readonly Mock<IEmailHelper> _mockEmailHelper;
    private readonly Mock<IEmailBuilder> _mockEmailBuilder;
    private readonly MessageBuilder _sut;

    public MessageBuilderTests()
    {
        _mockEmailHelper = new Mock<IEmailHelper>();
        _mockEmailBuilder = new Mock<IEmailBuilder>();
        _mockEmailHelper
            .Setup(h => h.ForType(It.IsAny<string>()))
            .Returns(_mockEmailBuilder.Object);
        _mockEmailBuilder
            .Setup(h => h.Map(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(_mockEmailBuilder.Object);

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

        _mockEmailBuilder
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(expectedMessage);

        var result = _sut.BuildTestNotification(toAddresses, smtpSettings);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.TestMail), Times.Once);
        Assert.Equal(expectedMessage, result);
    }

    [Theory]
    [InlineData(SecureSocketOptions.StartTls)]
    [InlineData(SecureSocketOptions.SslOnConnect)]
    [InlineData(SecureSocketOptions.Auto)]
    public void BuildTestNotification_RendersSmtpSecurityMode(SecureSocketOptions socketOptions)
    {
        var helper = new EmailHelper(Path.Join(AppContext.BaseDirectory, "mailConfiguration.xml"));
        var builder = new MessageBuilder(helper);
        var settings = new EmailSettings
        {
            SmtpSettings = new SmtpSettings("smtp.example.com", "sender@example.com", "password", 587)
            {
                SocketOptions = socketOptions
            }
        };

        var message = builder.BuildTestNotification(["reader@example.com"], settings);

        Assert.Contains($"{socketOptions}</td>", message.Body);
        Assert.Contains("587</td>", message.Body);
        Assert.DoesNotContain("{SocketOptions.Value}", message.Body);
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

        _mockEmailBuilder
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildNewCommentNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.NewCommentNotification), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.Username), payload.Username), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.Email), payload.Email), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.IpAddress), payload.IpAddress), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.PostTitle), payload.PostTitle), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.CommentContent), payload.CommentContent), Times.Once);
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

        _mockEmailBuilder
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildCommentReplyNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.AdminReplyNotification), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.ReplyContentHtml), payload.ReplyContentHtml), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.PostLink), payload.PostLink), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.Title), payload.Title), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.CommentContent), payload.CommentContent), Times.Once);
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

        _mockEmailBuilder
            .Setup(h => h.BuildMessage(It.IsAny<string[]>(), It.IsAny<string[]>()))
            .Returns(new CommonMailMessage());

        _sut.BuildPingNotification(toAddresses, payload);

        _mockEmailHelper.Verify(h => h.ForType(MessageTypes.BeingPinged), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.TargetPostTitle), payload.TargetPostTitle), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.Domain), payload.Domain), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.SourceIp), payload.SourceIp), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.SourceUrl), payload.SourceUrl), Times.Once);
        _mockEmailBuilder.Verify(h => h.Map(nameof(payload.SourceTitle), payload.SourceTitle), Times.Once);
    }
}
