using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Auditing
{
    public class AuditEntry
    {
        public EventId EventId { get; }

        public EventType EventType { get; }

        public DateTime EventTimeUtc { get; }

        public string Username { get; }

        public string IpAddressV4 { get; }

        public string MachineName { get; set; }

        public string Message { get; }

        public AuditEntry(EventType eventType, EventId eventId, string username, string ipAddressV4, string machineName, string message)
        {
            EventId = eventId;
            EventType = eventType;

            Username = username;
            Message = message;
            MachineName = machineName;
            IpAddressV4 = ipAddressV4;

            EventTimeUtc = DateTime.UtcNow;
        }
    }
}
