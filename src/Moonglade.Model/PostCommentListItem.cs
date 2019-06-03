using System;
using System.Collections.Generic;

namespace Moonglade.Model
{
    public class PostCommentListItem
    {
        public string Username { get; set; }
        public DateTime CreateOnUtc { get; set; }
        public string CommentContent { get; set; }
        public IReadOnlyList<CommentReplyDigest> CommentReplies { get; set; }
    }
}