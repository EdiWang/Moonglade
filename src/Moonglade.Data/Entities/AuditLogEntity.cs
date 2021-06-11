using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Data.Entities
{
    public class AuditLogEntity
    {
        public long Id { get; set; }

        public int EventId { get; set; }

        public int EventType { get; set; }

        public DateTime EventTimeUtc { get; set; }

        public string WebUsername { get; set; }

        public string IpAddressV4 { get; set; }

        public string MachineName { get; set; }

        public string Message { get; set; }
    }
}
