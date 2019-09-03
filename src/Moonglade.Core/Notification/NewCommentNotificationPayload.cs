using System;

namespace Moonglade.Core.Notification
{
    internal class NewCommentNotificationPayload
    {
        public NewCommentNotificationPayload(
            string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createOnUtc)
        {
            Username = username;
            Email = email;
            IpAddress = ipAddress;
            PostTitle = postTitle;
            CommentContent = commentContent;
            CreateOnUtc = createOnUtc;
        }

        public string Username { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public string PostTitle { get; set; }

        public string CommentContent { get; set; }

        public DateTime CreateOnUtc { get; set; }
    }
}