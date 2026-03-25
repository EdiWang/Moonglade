using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class CommentPagingSepc : Specification<CommentEntity>
{
    public CommentPagingSepc(int pageSize, int pageIndex, CommentFilter filter)
    {
        var startRow = (pageIndex - 1) * pageSize;

        ApplyFilter(filter);

        Query.Include(c => c.Post);
        Query.Include(c => c.Replies);

        Query.OrderByDescending(p => p.CreateTimeUtc);
        Query.Take(pageSize).Skip(startRow);
    }

    private void ApplyFilter(CommentFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            Query.Where(p => p.Username.Contains(filter.Username));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            Query.Where(p => p.Email.Contains(filter.Email));
        }

        if (!string.IsNullOrWhiteSpace(filter.CommentContent))
        {
            Query.Where(p => p.CommentContent.Contains(filter.CommentContent));
        }

        if (filter.StartTimeUtc.HasValue)
        {
            Query.Where(p => p.CreateTimeUtc >= filter.StartTimeUtc.Value);
        }

        if (filter.EndTimeUtc.HasValue)
        {
            Query.Where(p => p.CreateTimeUtc <= filter.EndTimeUtc.Value);
        }
    }
}

public sealed class CommentCountSpec : Specification<CommentEntity>
{
    public CommentCountSpec(CommentFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            Query.Where(p => p.Username.Contains(filter.Username));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            Query.Where(p => p.Email.Contains(filter.Email));
        }

        if (!string.IsNullOrWhiteSpace(filter.CommentContent))
        {
            Query.Where(p => p.CommentContent.Contains(filter.CommentContent));
        }

        if (filter.StartTimeUtc.HasValue)
        {
            Query.Where(p => p.CreateTimeUtc >= filter.StartTimeUtc.Value);
        }

        if (filter.EndTimeUtc.HasValue)
        {
            Query.Where(p => p.CreateTimeUtc <= filter.EndTimeUtc.Value);
        }
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