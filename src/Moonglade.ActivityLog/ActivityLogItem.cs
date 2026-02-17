namespace Moonglade.ActivityLog;

public class ActivityLogItem
{
    public long Id { get; set; }
    public EventType EventType { get; set; }
    public DateTime EventTimeUtc { get; set; }
    public string ActorId { get; set; }
    public string Operation { get; set; }
    public string TargetName { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}
