using Azure;
using Azure.Communication.Email;

namespace Moonglade.Email.Core;

public interface IAzureCommunicationEmailClient
{
    Task<string> SendAsync(WaitUntil waitUntil, EmailMessage message);
}
