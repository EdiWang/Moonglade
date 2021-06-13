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
    }
}
