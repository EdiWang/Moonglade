using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client;

public interface IMoongladeNotification
{
    Task<Guid> EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class;
}

public class MoongladeNotification : IMoongladeNotification
{
    public Task<Guid> EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class
    {
        throw new NotImplementedException();
    }
}