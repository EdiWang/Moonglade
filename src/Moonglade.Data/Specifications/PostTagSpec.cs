using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostTagSpec : Specification<PostTagEntity>
{
    public PostTagSpec(int tagId, int pageSize, int pageIndex)
    {
        Query.Where(pt =>
            pt.TagId == tagId
            && !pt.Post.IsDeleted
            && pt.Post.IsPublished);

        var startRow = (pageIndex - 1) * pageSize;
        Query.Skip(startRow).Take(pageSize);
        Query.OrderByDescending(p => p.Post.PubDateUtc);
    }
}

public class PostTagByTagIdSpec : SingleResultSpecification<PostTagEntity>
{
    public PostTagByTagIdSpec(int tagId)
    {
        Query.Where(pt => pt.TagId == tagId);
    }
}