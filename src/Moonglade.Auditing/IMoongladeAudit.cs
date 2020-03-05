using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Auditing
{
    public interface IMoongladeAudit
    {
        Task<Response> AddAuditEntry(EventType eventType, EventId eventId, string message);

        Task<Response<(IReadOnlyList<AuditEntry> Entries, int Count)>> GetAuditEntries(
            int skip, int take, EventType? eventType = null, EventId? eventId = null);

        Task<Response> ClearAuditLog();
    }
}