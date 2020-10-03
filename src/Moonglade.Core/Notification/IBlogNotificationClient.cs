using System;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Moonglade.Model;
using Moonglade.Pingback;

namespace Moonglade.Core.Notification
{
    public interface IBlogNotificationClient
    {
        Task<Response> TestNotificationAsync();

        Task NotifyNewCommentAsync(CommentDetailedItem model, Func<string, string> funcCommentContentFormat);

        Task NotifyCommentReplyAsync(CommentReplyDetail model, string postLink);

        Task NotifyPingbackAsync(PingbackHistory model);
    }
}
