using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class CommentPagingSepc : Specification<CommentEntity>
{
    public CommentPagingSepc(int pageSize, int pageIndex, string keyword)
    {
        var startRow = (pageIndex - 1) * pageSize;

        Query.Where(p => null == keyword || p.Username.Contains(keyword) || p.Email.Contains(keyword));

        Query.Include(c => c.Post);
        Query.Include(c => c.Replies);

        Query.OrderByDescending(p => p.CreateTimeUtc);
        Query.Take(pageSize).Skip(startRow);
    }
}

public sealed class CommentCountSpec : Specification<CommentEntity>
{
    public CommentCountSpec(string keyword)
    {
        Query.Where(p => null == keyword || p.Username.Contains(keyword) || p.Email.Contains(keyword));
    }
}

public sealed class CommentByIdsSepc : Specification<CommentEntity>
{
    public CommentByIdsSepc(Guid[] ids)
    {
        Query.Where(c => ids.Contains(c.Id));
    }
}

public sealed class CommentWithRepliesSpec : Specification<CommentEntity>
{
    public CommentWithRepliesSpec(Guid postId)
    {
        Query.Where(c => c.PostId == postId && c.IsApproved);
        Query.Include(p => p.Replies);
    }
}

public class CommentEntityToCommentSpec : Specification<CommentEntity, Comment>
{
    public CommentEntityToCommentSpec()
    {
        Query.Select(p => new Comment
        {
            Username = p.Username,
            Email = p.Email,
            CreateTimeUtc = p.CreateTimeUtc,
            CommentContent = p.CommentContent,
            Replies = p.Replies.Select(cr => new CommentReplyDigest
            {
                ReplyContent = cr.ReplyContent,
                ReplyTimeUtc = cr.CreateTimeUtc
            }).ToList()
        });
    }
}

public class CommentEntityToCommentDetailedItemSpec : Specification<CommentEntity, CommentDetailedItem>
{
    public CommentEntityToCommentDetailedItemSpec()
    {
        Query.Select(p => new CommentDetailedItem
        {
            Id = p.Id,
            CommentContent = p.CommentContent,
            CreateTimeUtc = p.CreateTimeUtc,
            Email = p.Email,
            IpAddress = p.IPAddress,
            Username = p.Username,
            IsApproved = p.IsApproved,
            PostTitle = p.Post.Title,
            Replies = p.Replies.Select(cr => new CommentReplyDigest
            {
                ReplyContent = cr.ReplyContent,
                ReplyTimeUtc = cr.CreateTimeUtc
            }).ToList()
        });
    }
}

public sealed class CommentWithPostByIdSpec : Specification<CommentEntity>
{
    public CommentWithPostByIdSpec(Guid commentId)
    {
        Query.Where(c => c.Id == commentId);
        Query.Include(c => c.Post);
    }
}