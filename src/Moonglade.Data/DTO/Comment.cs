namespace Moonglade.Data.DTO;

public record Comment
{
    public string Username { get; init; }

    public string Email { get; init; }

    public DateTime CreateTimeUtc { get; init; }

    public string CommentContent { get; init; }

    public List<CommentReplyDigest> Replies { get; init; }
}

public record CommentDetailedItem : Comment
{
    public Guid Id { get; init; }
    public string IpAddress { get; init; }
    public string PostTitle { get; init; }
    public bool IsApproved { get; init; }
}