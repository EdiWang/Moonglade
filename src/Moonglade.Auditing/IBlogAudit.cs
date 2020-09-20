using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Auditing
{
    public interface IBlogAudit
    {
        Task<Response> AddAuditEntry(EventType eventType, AuditEventId auditEventId, string message);

        Task<Response<(IReadOnlyList<AuditEntry> Entries, int Count)>> GetAuditEntries(
            int skip, int take, EventType? eventType = null, AuditEventId? eventId = null);

        Task<Response> ClearAuditLog();
    }
}