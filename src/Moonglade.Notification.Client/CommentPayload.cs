namespace Moonglade.Notification.Client
{
    internal class CommentPayload
    {
        public CommentPayload(
            string username, string email, string ipAddress, string postTitle, string commentContent, DateTime createTimeUtc)
        {
            Username = username;
            Email = email;
            IpAddress = ipAddress;
            PostTitle = postTitle;
            CommentContent = commentContent;
            CreateTimeUtc = createTimeUtc;
        }

        public string Username { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public string PostTitle { get; set; }

        public string CommentContent { get; set; }

        public DateTime CreateTimeUtc { get; set; }
    }
}