namespace Moonglade.Data.Entities;

public class ActivityLogEntity
{
    public long Id { get; set; }
    public int EventId { get; set; }
    public DateTime? EventTimeUtc { get; set; }
    public string ActorId { get; set; }
    public string Operation { get; set; }
    public string TargetName { get; set; }
    public string MetaData { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}
