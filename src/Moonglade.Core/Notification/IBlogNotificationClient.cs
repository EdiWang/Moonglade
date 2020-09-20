using System;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Model;

namespace Moonglade.Core.Notification
{
    public interface IBlogNotificationClient
    {
        Task<Response> SendTestNotificationAsync();

        Task SendNewCommentNotificationAsync(CommentDetailedItem model, Func<string, string> funcCommentContentFormat);

        Task SendCommentReplyNotificationAsync(CommentReplyDetail model, string postLink);

        Task SendPingNotificationAsync(PingbackHistory model);
    }
}
