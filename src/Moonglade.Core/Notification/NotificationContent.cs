using System.Net.Http;
using System.Text;
using Moonglade.Configuration;

namespace Moonglade.Core.Notification
{
    internal class NotificationContent<T> : StringContent where T : class
    {
        public NotificationContent(NotificationRequest<T> req) :
            base(req.ToJson(), Encoding.UTF8, "application/json")
        { }
    }
}