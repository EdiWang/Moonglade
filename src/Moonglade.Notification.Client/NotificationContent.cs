using Moonglade.Configuration;
using System.Text;

namespace Moonglade.Notification.Client
{
    internal class NotificationContent<T> : StringContent where T : class
    {
        public NotificationContent(NotificationRequest<T> req) :
            base(req.ToJson(), Encoding.UTF8, "application/json")
        { }
    }
}