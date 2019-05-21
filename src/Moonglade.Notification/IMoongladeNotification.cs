using System;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Data.Entities;
using Moonglade.Model;

namespace Moonglade.Notification
{
    public interface IMoongladeNotification
    {
        bool IsEnabled { get; set; }

        Task<Response> SendTestNotificationAsync();

        Task SendNewCommentNotificationAsync(Comment comment, string postTitle,
            Func<string, string> funcCommentContentFormat);

        Task SendCommentReplyNotification(CommentReplySummary model, string postLink);

        Task SendPingNotification(PingbackHistoryEntity receivedPingback, string postTitle);
    }
}
