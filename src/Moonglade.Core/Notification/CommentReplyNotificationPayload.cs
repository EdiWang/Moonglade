namespace Moonglade.Core.Notification
{
    internal class CommentReplyNotificationPayload
    {
        public CommentReplyNotificationPayload(
            string email, string commentContent, string title, string replyContent, string postLink)
        {
            Email = email;
            CommentContent = commentContent;
            Title = title;
            ReplyContent = replyContent;
            PostLink = postLink;
        }

        public string Email { get; set; }

        public string CommentContent { get; set; }

        public string Title { get; set; }

        public string ReplyContent { get; set; }

        public string PostLink { get; set; }
    }
}