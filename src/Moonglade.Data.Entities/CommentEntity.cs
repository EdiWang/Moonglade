using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class CommentEntity
    {
        public CommentEntity()
        {
            CommentReply = new HashSet<CommentReplyEntity>();
        }

        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string IPAddress { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public string CommentContent { get; set; }
        public Guid PostId { get; set; }
        public bool IsApproved { get; set; }
        public string UserAgent { get; set; }

        public virtual PostEntity Post { get; set; }
        public virtual ICollection<CommentReplyEntity> CommentReply { get; set; }
    }
}
