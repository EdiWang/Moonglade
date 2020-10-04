using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Auditing
{
    public interface IBlogAudit
    {
        Task AddAuditEntry(EventType eventType, AuditEventId auditEventId, string message);

        Task<(IReadOnlyList<AuditEntry> Entries, int Count)> GetAuditEntries(
            int skip, int take, EventType? eventType = null, AuditEventId? eventId = null);

        Task ClearAuditLog();
    }
}