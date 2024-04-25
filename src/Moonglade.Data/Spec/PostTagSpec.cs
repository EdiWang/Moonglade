using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public sealed class PostTagSpec : BaseSpecification<PostTagEntity>
{
    public PostTagSpec(int tagId, int pageSize, int pageIndex)
        : base(pt =>
            pt.TagId == tagId
            && !pt.Post.IsDeleted
            && pt.Post.IsPublished)
    {
        var startRow = (pageIndex - 1) * pageSize;
        ApplyPaging(startRow, pageSize);
        ApplyOrderByDescending(p => p.Post.PubDateUtc);
    }
}

public class PostTagByTagIdSpec : Specification<PostTagEntity>
{
    public PostTagByTagIdSpec(int tagId)
    {
        Query.Where(pt => pt.TagId == tagId);
    }
}