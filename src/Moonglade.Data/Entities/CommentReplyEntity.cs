using System;

namespace Moonglade.Data.Entities
{
    public class CommentReplyEntity
    {
        public Guid Id { get; set; }
        public string ReplyContent { get; set; }
        public DateTime ReplyTimeUtc { get; set; }
        public string UserAgent { get; set; }
        public string IpAddress { get; set; }
        public Guid? CommentId { get; set; }

        public virtual CommentEntity Comment { get; set; }
    }
}
