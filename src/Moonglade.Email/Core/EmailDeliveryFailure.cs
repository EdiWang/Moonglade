namespace Moonglade.Email.Core;

public record EmailDeliveryFailure(string Recipient, EmailDeliveryFailureKind Kind, Exception Exception);
