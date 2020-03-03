using System.Collections.Generic;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;

namespace Moonglade.Auditing
{
    public interface IMoongladeAudit
    {
        Task<Response> AddAuditEntry(EventType eventType, EventId eventId, string message);

        Task<Response<IReadOnlyList<AuditEntry>>> GetAuditEntries(
            int skip, int take, EventType? eventType, EventId? eventId, bool orderByTimeDesc = true);
    }
}