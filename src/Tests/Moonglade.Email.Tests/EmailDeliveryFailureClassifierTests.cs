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
    [InlineData(SmtpStatusCode.MailboxBusy, EmailDeliveryFailureKind.Transient)]
    [InlineData(SmtpStatusCode.ErrorInProcessing, EmailDeliveryFailureKind.Transient)]
    [InlineData(SmtpStatusCode.InsufficientStorage, EmailDeliveryFailureKind.Transient)]
    [InlineData(SmtpStatusCode.TransactionFailed, EmailDeliveryFailureKind.Transient)]
    [InlineData(SmtpStatusCode.AuthenticationRequired, EmailDeliveryFailureKind.Permanent)]
    public void TryClassify_SmtpCommandException_UsesStatusCode(SmtpStatusCode statusCode, EmailDeliveryFailureKind expectedKind)
    {
        var exception = new SmtpCommandException(SmtpErrorCode.MessageNotAccepted, statusCode, "SMTP command failed");

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
