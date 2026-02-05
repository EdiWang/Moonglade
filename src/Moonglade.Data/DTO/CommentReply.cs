namespace Moonglade.Data.DTO;

public record CommentReplyDigest
{
    public DateTime ReplyTimeUtc { get; init; }
    public string ReplyContent { get; init; }
    public string ReplyContentHtml { get; init; }
}

public record CommentReply : CommentReplyDigest
{
    public Guid Id { get; init; }
    public Guid CommentId { get; init; }
    public Guid PostId { get; init; }
    public string Email { get; init; }
    public string CommentContent { get; init; }
    public string Title { get; init; }
    public string RouteLink { get; init; }
}