using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Moonglade.Core.Notification
{
    internal class NotificationContent<T> : StringContent where T : class
    {
        public NotificationContent(NotificationRequest<T> req) :
            base(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json")
        { }
    }
}