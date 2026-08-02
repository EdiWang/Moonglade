using Azure;
using Azure.Communication.Email;
using Edi.TemplateEmail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Email.Core;
using Moq;

namespace Moonglade.Email.Tests;

public class AzureCommunicationSenderTests
{
    private readonly Mock<IAzureCommunicationEmailClient> _mockClient = new();
    private readonly Mock<ILogger<AzureCommunicationSender>> _mockLogger = new();

    private AzureCommunicationSender CreateSut(string senderAddress = "no-reply@example.com")
    {
        _mockClient
            .Setup(c => c.SendAsync(It.IsAny<WaitUntil>(), It.IsAny<EmailMessage>()))
            .ReturnsAsync("operation-id");

        return new AzureCommunicationSender(
            Options.Create(new EmailServiceOptions { AcsSenderAddress = senderAddress }),
            _mockClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Provider_ReturnsAzureCommunicationProvider()
    {
        var sut = CreateSut();

        Assert.Equal(EmailServiceOptions.AzureCommunicationProvider, sut.Provider);
    }

    [Fact]
    public async Task SendAsync_UsesStartedWaitMode()
    {
        var sut = CreateSut();
        var message = new CommonMailMessage
        {
            Subject = "Subject",
            Receipts = ["user@example.com"],
            Body = "Body"
        };

        await sut.SendAsync(message);

        _mockClient.Verify(c => c.SendAsync(WaitUntil.Started, It.IsAny<EmailMessage>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_MapsHtmlMessage()
    {
        var sut = CreateSut();
        EmailMessage capturedMessage = null;
        _mockClient
            .Setup(c => c.SendAsync(It.IsAny<WaitUntil>(), It.IsAny<EmailMessage>()))
            .Callback<WaitUntil, EmailMessage>((_, emailMessage) => capturedMessage = emailMessage)
            .ReturnsAsync("operation-id");

        var message = new CommonMailMessage
        {
            Subject = "Subject",
            Receipts = ["user@example.com"],
            Body = "<p>Hello</p>",
            BodyIsHtml = true
        };

        await sut.SendAsync(message);

        Assert.NotNull(capturedMessage);
        Assert.Equal("no-reply@example.com", capturedMessage.SenderAddress);
        Assert.Equal("Subject", capturedMessage.Content.Subject);
        Assert.Equal("<p>Hello</p>", capturedMessage.Content.Html);
        Assert.Null(capturedMessage.Content.PlainText);
        Assert.Contains(capturedMessage.Recipients.To, address => address.Address == "user@example.com");
    }

    [Fact]
    public async Task SendAsync_MapsPlainTextMessage()
    {
        var sut = CreateSut();
        EmailMessage capturedMessage = null;
        _mockClient
            .Setup(c => c.SendAsync(It.IsAny<WaitUntil>(), It.IsAny<EmailMessage>()))
            .Callback<WaitUntil, EmailMessage>((_, emailMessage) => capturedMessage = emailMessage)
            .ReturnsAsync("operation-id");

        var message = new CommonMailMessage
        {
            Subject = "Subject",
            Receipts = ["user@example.com"],
            Body = "Hello",
            BodyIsHtml = false
        };

        await sut.SendAsync(message);

        Assert.NotNull(capturedMessage);
        Assert.Equal("Hello", capturedMessage.Content.PlainText);
        Assert.Null(capturedMessage.Content.Html);
    }
}
