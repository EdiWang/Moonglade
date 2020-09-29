using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ProcessReceivedPayloadAsync(string requestBody, string ip, Action<PingbackHistory> pingSuccessAction);
        Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}