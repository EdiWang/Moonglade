using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ProcessReceivedPayloadAsync(HttpContext context, Action<PingbackHistory> pingSuccessAction);
        Task<IEnumerable<PingbackHistory>> GetPingbackHistoryAsync();
        Task DeletePingbackHistory(Guid id);
    }
}