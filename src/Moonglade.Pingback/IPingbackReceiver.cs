using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Moonglade.Pingback
{
    public interface IPingbackReceiver
    {
        ILogger<PingbackReceiver> Logger { get; set; }
        int RemoteTimeout { get; set; }
        event PingbackReceiver.PingSuccessHandler OnPingSuccess;
        PingbackValidationResult ValidatePingRequest(string requestBody, string remoteIp);
        Task<PingRequest> GetPingRequest();
        PingbackResponse ReceivingPingback(PingRequest req, Func<bool> ifTargetResourceExists, Func<bool> ifAlreadyBeenPinged);
    }
}