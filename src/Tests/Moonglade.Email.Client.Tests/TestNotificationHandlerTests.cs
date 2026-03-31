using Moonglade.Configuration;
using Moq;

namespace Moonglade.Email.Client.Tests;

public class TestNotificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_SendsTestMailToOwner()
    {
        var emailClientMock = new Mock<IMoongladeEmailClient>();
        var blogConfigMock = new Mock<IBlogConfig>();
        blogConfigMock.Setup(c => c.GeneralSettings).Returns(new GeneralSettings
        {
            OwnerEmail = "owner@blog.com"
        });

        var handler = new TestNotificationHandler(emailClientMock.Object, blogConfigMock.Object);

        await handler.HandleAsync(new TestEmailEvent(), CancellationToken.None);

        emailClientMock.Verify(
            c => c.SendEmailAsync(
                MailMesageTypes.TestMail,
                It.Is<string[]>(r => r.Length == 1 && r[0] == "owner@blog.com"),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
