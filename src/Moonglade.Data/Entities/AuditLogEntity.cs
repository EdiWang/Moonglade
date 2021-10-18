namespace Moonglade.Data.Entities;

public class AuditLogEntity
{
    public long Id { get; set; }

    public BlogEventId EventId { get; set; }

    public BlogEventType EventType { get; set; }

    public DateTime EventTimeUtc { get; set; }

    public string WebUsername { get; set; }

    public string IpAddressV4 { get; set; }

    public string MachineName { get; set; }

    public string Message { get; set; }
}