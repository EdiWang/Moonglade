using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moonglade.Data.Entities;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackEntity> pingSuccessAction);
        Task<IReadOnlyList<PingbackEntity>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}