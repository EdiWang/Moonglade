using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackHistory> pingSuccessAction);
        Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}