using Azure;
using MailKit;
using MailKit.Net.Smtp;
using System.Net;

namespace Moonglade.Email.Core;

public static class EmailDeliveryFailureClassifier
{
    public static bool TryClassify(Exception exception, out EmailDeliveryFailureKind kind)
    {
        switch (exception)
        {
            case SmtpCommandException smtpCommandException:
                kind = ClassifySmtpCommandException(smtpCommandException);
                return true;

            case SmtpProtocolException:
            case ServiceNotConnectedException:
            case ServiceNotAuthenticatedException:
                kind = EmailDeliveryFailureKind.Transient;
                return true;

            case RequestFailedException requestFailedException:
                kind = ClassifyRequestFailedException(requestFailedException);
                return true;

            case TimeoutException:
                kind = EmailDeliveryFailureKind.Transient;
                return true;

            default:
                kind = EmailDeliveryFailureKind.Transient;
                return false;
        }
    }

    private static EmailDeliveryFailureKind ClassifySmtpCommandException(SmtpCommandException exception)
    {
        return exception.StatusCode switch
        {
            SmtpStatusCode.ServiceNotAvailable or
            SmtpStatusCode.MailboxBusy or
            SmtpStatusCode.ErrorInProcessing or
            SmtpStatusCode.ExceededStorageAllocation or
            SmtpStatusCode.InsufficientStorage or
            SmtpStatusCode.TransactionFailed => EmailDeliveryFailureKind.Transient,
            _ => EmailDeliveryFailureKind.Permanent
        };
    }

    private static EmailDeliveryFailureKind ClassifyRequestFailedException(RequestFailedException exception)
    {
        return exception.Status switch
        {
            (int)HttpStatusCode.RequestTimeout or
            (int)HttpStatusCode.TooManyRequests => EmailDeliveryFailureKind.Transient,
            >= 500 => EmailDeliveryFailureKind.Transient,
            >= 400 => EmailDeliveryFailureKind.Permanent,
            _ => EmailDeliveryFailureKind.Transient
        };
    }
}
