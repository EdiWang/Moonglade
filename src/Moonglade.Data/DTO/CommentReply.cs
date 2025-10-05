﻿namespace Moonglade.Data.DTO;

public class CommentReplyDigest
{
    public DateTime ReplyTimeUtc { get; set; }
    public string ReplyContent { get; set; }
    public string ReplyContentHtml { get; set; }
}

public class CommentReply : CommentReplyDigest
{
    public Guid Id { get; set; }
    public Guid CommentId { get; set; }
    public Guid PostId { get; set; }
    public string Email { get; set; }
    public string CommentContent { get; set; }
    public string Title { get; set; }
    public string RouteLink { get; set; }
}