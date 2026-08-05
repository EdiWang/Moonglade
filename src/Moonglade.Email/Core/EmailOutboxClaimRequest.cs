namespace Moonglade.Email.Core;

public sealed record EmailOutboxClaimRequest(
    string WorkerId,
    DateTime UtcNow,
    TimeSpan LeaseDuration);
