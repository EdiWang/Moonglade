using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class CommentListItem
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string IpAddress { get; set; }
        public string CommentContent { get; set; }
        public string PostTitle { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public IReadOnlyList<CommentReplyDigest> CommentReplies { get; set; }
    }
}
