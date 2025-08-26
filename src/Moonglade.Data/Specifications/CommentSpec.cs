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

public sealed class CommentWithPostByIdSpec : Specification<CommentEntity>
{
    public CommentWithPostByIdSpec(Guid commentId)
    {
        Query.Where(c => c.Id == commentId);
        Query.Include(c => c.Post);
    }
}