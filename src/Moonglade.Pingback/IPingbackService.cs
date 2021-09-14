using Moonglade.Data.Entities;
using System;
using System.Threading.Tasks;

namespace Moonglade.Pingback
{
    public interface IPingbackService
    {
        Task<PingbackResponse> ReceivePingAsync(string requestBody, string ip, Action<PingbackEntity> pingSuccessAction);
        Task DeletePingback(Guid id);
    }
}