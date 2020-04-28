using System;

namespace Moonglade.Auditing
{
    public class AuditEntry
    {
        public AuditEventId EventId { get; set; }

        public EventType EventType { get; set; }

        public DateTime EventTimeUtc { get; set; }

        public string Username { get; set; }

        public string IpAddressV4 { get; set; }

        public string MachineName { get; set; }

        public string Message { get; set; }

        public AuditEntry()
        {
            // For Dapper Mapping
        }

        public AuditEntry(EventType eventType, AuditEventId auditEventId, string username, string ipAddressV4, string machineName, string message)
        {
            EventId = auditEventId;
            EventType = eventType;

            Username = username;
            Message = message;
            MachineName = machineName;
            IpAddressV4 = ipAddressV4;

            EventTimeUtc = DateTime.UtcNow;
        }
    }
}
