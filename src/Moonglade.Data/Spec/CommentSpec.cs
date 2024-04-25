using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class CommentSpec : BaseSpecification<CommentEntity>
{
    public CommentSpec(int pageSize, int pageIndex) : base(c => true)
    {
        var startRow = (pageIndex - 1) * pageSize;

        AddInclude(comment => comment
            .Include(c => c.Post)
            .Include(c => c.Replies));
        ApplyOrderByDescending(p => p.CreateTimeUtc);
        ApplyPaging(startRow, pageSize);
    }

    public CommentSpec(Guid postId) : base(c => c.PostId == postId &&
                                                c.IsApproved)
    {
        AddInclude(comments => comments.Include(c => c.Replies));
    }
}

public class CommentByIdsSepc : Specification<CommentEntity>
{
    public CommentByIdsSepc(Guid[] ids)
    {
        Query.Where(c => ids.Contains(c.Id));
    }
}