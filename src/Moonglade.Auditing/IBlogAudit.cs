using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Auditing
{
    public interface IBlogAudit
    {
        Task AddAuditEntry(EventType eventType, BlogEventId blogEventId, string message);

        Task<(IReadOnlyList<AuditEntry> Entries, int Count)> GetAuditEntries(int skip, int take);

        Task ClearAuditLog();
    }
}