using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Moonglade.Notification.Client
{
    public interface IBlogNotificationClient
    {
        Task<HttpResponseMessage> SendNotification<T>(MailMesageTypes type, T payload) where T : class;

        Task NotifyCommentAsync(string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createTimeUtc);

        Task NotifyCommentReplyAsync(string email, string commentContent, string title, string replyContentHtml, string postLink);
    }
}
