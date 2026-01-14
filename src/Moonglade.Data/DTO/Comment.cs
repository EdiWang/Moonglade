namespace Moonglade.Data.DTO;

public class Comment
{
    public string Username { get; set; }

    public string Email { get; set; }

    public DateTime CreateTimeUtc { get; set; }

    public string CommentContent { get; set; }

    public List<CommentReplyDigest> Replies { get; set; }
}

public class CommentDetailedItem : Comment
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; }
    public string PostTitle { get; set; }
    public bool IsApproved { get; set; }
}