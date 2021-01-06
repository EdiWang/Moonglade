using System;
using System.Net;

namespace Moonglade.Model
{
    public class CommentRequest
    {
        public Guid PostId { get; }

        public string Username { get; set; }

        public string Content { get; set; }

        public string Email { get; set; }

        public IPAddress IpAddress { get; set; }

        public CommentRequest(Guid postId)
        {
            PostId = postId;
        }
    }
}
