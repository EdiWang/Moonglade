using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackRecord> pingSuccessAction);
        Task<IEnumerable<PingbackRecord>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}