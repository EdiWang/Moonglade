#nullable enable

namespace Moonglade.Data.Entities;

public class ActivityLogEntity
{
    public long Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public DateTime EventTimeUtc { get; set; }
    public ActivityActorKind ActorKind { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? TargetId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
}

public enum ActivityActorKind
{
    Administrator = 1,
    System = 2
}
