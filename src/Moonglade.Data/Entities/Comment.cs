using System;
using System.Collections.Generic;

namespace Moonglade.Data.Entities
{
    public class Comment
    {
        public Comment()
        {
            CommentReply = new HashSet<CommentReply>();
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

        public virtual Post Post { get; set; }
        public virtual ICollection<CommentReply> CommentReply { get; set; }
    }
}
