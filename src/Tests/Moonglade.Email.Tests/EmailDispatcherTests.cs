using Edi.TemplateEmail;
using Microsoft.Extensions.Options;
using Moonglade.Email.Core;
using Moq;

namespace Moonglade.Email.Tests;

public class EmailDispatcherTests
{
    private readonly Mock<IEmailProviderSender> _mockSmtpSender = new();
    private readonly Mock<IEmailProviderSender> _mockAcsSender = new();

    public EmailDispatcherTests()
    {
        _mockSmtpSender.SetupGet(s => s.Provider).Returns(EmailServiceOptions.SmtpProvider);
        _mockSmtpSender.Setup(s => s.SendAsync(It.IsAny<CommonMailMessage>())).Returns(Task.CompletedTask);

        _mockAcsSender.SetupGet(s => s.Provider).Returns(EmailServiceOptions.AzureCommunicationProvider);
        _mockAcsSender.Setup(s => s.SendAsync(It.IsAny<CommonMailMessage>())).Returns(Task.CompletedTask);
    }

    private EmailDispatcher CreateDispatcher(string provider)
    {
        return new EmailDispatcher(
            Options.Create(new EmailServiceOptions { Provider = provider }),
            [_mockSmtpSender.Object, _mockAcsSender.Object]);
    }

    [Fact]
    public async Task SendAsync_UnsupportedProvider_ThrowsInvalidOperationException()
    {
        var dispatcher = CreateDispatcher("unsupported");
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.SendAsync(message));
    }

    [Fact]
    public async Task SendAsync_NullOrEmptyProvider_DefaultsToSmtpSender()
    {
        var dispatcher = CreateDispatcher(string.Empty);
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await dispatcher.SendAsync(message);

        _mockSmtpSender.Verify(s => s.SendAsync(message), Times.Once);
        _mockAcsSender.Verify(s => s.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_ProviderIsCaseInsensitive_RoutesToSmtpSender()
    {
        var dispatcher = CreateDispatcher("SMTP");
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await dispatcher.SendAsync(message);

        _mockSmtpSender.Verify(s => s.SendAsync(message), Times.Once);
        _mockAcsSender.Verify(s => s.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_AzureCommunicationProvider_RoutesToAcsSender()
    {
        var dispatcher = CreateDispatcher("AzureCommunication");
        var message = new CommonMailMessage { Subject = "Test", Receipts = ["test@example.com"] };

        await dispatcher.SendAsync(message);

        _mockAcsSender.Verify(s => s.SendAsync(message), Times.Once);
        _mockSmtpSender.Verify(s => s.SendAsync(It.IsAny<CommonMailMessage>()), Times.Never);
    }
}
