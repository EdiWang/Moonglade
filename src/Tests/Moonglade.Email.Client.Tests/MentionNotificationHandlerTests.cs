using Moonglade.Configuration;
using Moq;

namespace Moonglade.Email.Client.Tests;

public class MentionNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsMentionEmailToOwner()
    {
        var emailClientMock = new Mock<IMoongladeEmailClient>();
        var blogConfigMock = new Mock<IBlogConfig>();
        blogConfigMock.Setup(c => c.GeneralSettings).Returns(new GeneralSettings
        {
            OwnerEmail = "owner@blog.com"
        });

        var handler = new MentionNotificationHandler(emailClientMock.Object, blogConfigMock.Object);

        var evt = new MentionEvent("Target Post", "source.com", "1.2.3.4", "https://source.com/post", "Source Title");

        await handler.HandleAsync(evt, CancellationToken.None);

        emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.BeingPinged,
                It.Is<string[]>(r => r.Length == 1 && r[0] == "owner@blog.com"),
                It.Is<MentionEvent>(e =>
                    e.TargetPostTitle == "Target Post" &&
                    e.Domain == "source.com" &&
                    e.SourceIp == "1.2.3.4" &&
                    e.SourceUrl == "https://source.com/post" &&
                    e.SourceTitle == "Source Title"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
