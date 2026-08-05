namespace Moonglade.Data.Entities;

public enum EmailOutboxMessageStatus
{
    Pending = 0,
    Processing = 1,
    Succeeded = 2,
    Failed = 3,
    DeadLettered = 4
}
