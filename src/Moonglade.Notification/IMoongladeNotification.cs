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

        Task SendNewCommentNotificationAsync(CommentEntity commentEntity, string postTitle,
            Func<string, string> funcCommentContentFormat);

        Task SendCommentReplyNotificationAsync(CommentReplySummary model, string postLink);

        Task SendPingNotificationAsync(PingbackHistoryEntity receivedPingback, string postTitle);
    }
}
