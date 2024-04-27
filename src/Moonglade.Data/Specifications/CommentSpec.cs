using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class CommentPagingSepc : Specification<CommentEntity>
{
    public CommentPagingSepc(int pageSize, int pageIndex)
    {
        var startRow = (pageIndex - 1) * pageSize;

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