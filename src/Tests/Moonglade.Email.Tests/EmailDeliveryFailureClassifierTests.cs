using Azure;
using MailKit.Net.Smtp;
using Moonglade.Email.Core;

namespace Moonglade.Email.Tests;

public class EmailDeliveryFailureClassifierTests
{
    [Fact]
    public void TryClassify_SmtpMailboxUnavailable_IsPermanent()
    {
        var exception = new SmtpCommandException(
            SmtpErrorCode.RecipientNotAccepted,
            SmtpStatusCode.MailboxUnavailable,
            "Mailbox unavailable");

        var classified = EmailDeliveryFailureClassifier.TryClassify(exception, out var kind);

        Assert.True(classified);
        Assert.Equal(EmailDeliveryFailureKind.Permanent, kind);
    }

    [Fact]
    public void TryClassify_SmtpServiceNotAvailable_IsTransient()
    {
        var exception = new SmtpCommandException(
            SmtpErrorCode.MessageNotAccepted,
            SmtpStatusCode.ServiceNotAvailable,
            "Service unavailable");

        var classified = EmailDeliveryFailureClassifier.TryClassify(exception, out var kind);

        Assert.True(classified);
        Assert.Equal(EmailDeliveryFailureKind.Transient, kind);
    }

    [Theory]
    [InlineData(400, EmailDeliveryFailureKind.Permanent)]
    [InlineData(408, EmailDeliveryFailureKind.Transient)]
    [InlineData(429, EmailDeliveryFailureKind.Transient)]
    [InlineData(503, EmailDeliveryFailureKind.Transient)]
    public void TryClassify_RequestFailedException_UsesStatusCode(int statusCode, EmailDeliveryFailureKind expectedKind)
    {
        var exception = new RequestFailedException(statusCode, "Request failed");

        var classified = EmailDeliveryFailureClassifier.TryClassify(exception, out var kind);

        Assert.True(classified);
        Assert.Equal(expectedKind, kind);
    }

    [Fact]
    public void TryClassify_UnexpectedException_IsUnknown()
    {
        var classified = EmailDeliveryFailureClassifier.TryClassify(new InvalidOperationException(), out _);

        Assert.False(classified);
    }
}
