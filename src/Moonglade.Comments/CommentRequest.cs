using System;

namespace Moonglade.Comments
{
    public class CommentRequest
    {
        public Guid PostId { get; }

        public string Username { get; set; }

        public string Content { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public CommentRequest(Guid postId)
        {
            PostId = postId;
        }
    }
}
