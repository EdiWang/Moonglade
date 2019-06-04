using System;

namespace Moonglade.Model
{
    public class NewCommentRequest
    {
        public Guid PostId { get; }

        public string Username { get; set; }

        public string Content { get; set; }

        public string Email { get; set; }

        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        public NewCommentRequest(Guid postId)
        {
            PostId = postId;
        }
    }
}
