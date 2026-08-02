namespace Moonglade.Email.Core;

public sealed record EmailOutboxMessage(
    Guid Id,
    string MessageType,
    string DistributionList,
    string MessageBody,
    int AttemptCount,
    DateTime CreatedTimeUtc);
