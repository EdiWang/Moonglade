using System;

namespace Moonglade.Model
{
    public class CommentGridModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string IpAddress { get; set; }
        public string CommentContent { get; set; }
        public string PostTitle { get; set; }
        public DateTime CreateOnUtc { get; set; }
    }
}
