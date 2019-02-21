using System;

namespace Moonglade.Model
{
    public class CommentReplySummary
    {
        public Guid Id { get; set; }
        public string ReplyContent { get; set; }
        public DateTime? ReplyTimeUtc { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public Guid? CommentId { get; set; }
        public string Email { get; set; }
        public string CommentContent { get; set; }
        public Guid PostId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public DateTime? PubDateUTC { get; set; }
    }
}
