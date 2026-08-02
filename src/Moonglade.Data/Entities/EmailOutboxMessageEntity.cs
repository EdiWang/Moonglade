namespace Moonglade.Data.Entities;

public class EmailOutboxMessageEntity
{
    public Guid Id { get; set; }
    public string MessageType { get; set; }
    public string DistributionList { get; set; }
    public string MessageBody { get; set; }
    public EmailOutboxMessageStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedTimeUtc { get; set; }
    public DateTime? LastAttemptTimeUtc { get; set; }
    public DateTime? NotBeforeUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public string LockedBy { get; set; }
    public DateTime? SentTimeUtc { get; set; }
    public string LastError { get; set; }
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
