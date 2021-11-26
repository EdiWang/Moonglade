using Moonglade.Data.Entities;
using System.Linq.Expressions;

namespace Moonglade.Comments;

public class Comment
{
    public string Username { get; set; }

    public string Email { get; set; }

    public DateTime CreateTimeUtc { get; set; }

    public string CommentContent { get; set; }

    public IReadOnlyList<CommentReplyDigest> CommentReplies { get; set; }
}

public class CommentDetailedItem : Comment
{
    public Guid Id { get; set; }
    public string IpAddress { get; set; }
    public string PostTitle { get; set; }
    public bool IsApproved { get; set; }

    public static Expression<Func<CommentEntity, CommentDetailedItem>> EntitySelector => p => new()
    {
        Id = p.Id,
        CommentContent = p.CommentContent,
        CreateTimeUtc = p.CreateTimeUtc,
        Email = p.Email,
        IpAddress = p.IPAddress,
        Username = p.Username,
        IsApproved = p.IsApproved,
        PostTitle = p.Post.Title,
        CommentReplies = p.Replies.Select(cr => new CommentReplyDigest
        {
            ReplyContent = cr.ReplyContent,
            ReplyTimeUtc = cr.CreateTimeUtc
        }).ToList()
    };
}