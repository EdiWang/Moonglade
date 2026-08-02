namespace Moonglade.Email.Core;

public sealed record EmailOutboxFailure(
    Guid MessageId,
    string ErrorMessage,
    DateTime FailedTimeUtc);
