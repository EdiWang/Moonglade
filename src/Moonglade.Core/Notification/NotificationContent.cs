using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Moonglade.Core.Notification
{
    internal class NotificationContent<T> : StringContent where T : class
    {
        public NotificationContent(NotificationRequest<T> req) :
            base(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        { }
    }
}