using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Moonglade.Core.Notification
{
    internal class NotificationContent : StringContent
    {
        public NotificationContent(NotificationRequest req) :
            base(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        { }
    }
}