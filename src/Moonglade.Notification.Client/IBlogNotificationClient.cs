using System.Net.Http;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client
{
    public interface IBlogNotificationClient
    {
        Task<HttpResponseMessage> SendNotification<T>(MailMesageTypes type, T payload) where T : class;

        Task NotifyCommentReplyAsync(string email, string commentContent, string title, string replyContentHtml, string postLink);
    }
}
