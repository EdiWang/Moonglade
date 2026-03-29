using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSpec : Specification<PostEntity>
{
    public PostSpec(Guid id, bool includeRelatedData = true)
    {
        Query.Where(p => p.Id == id);

        if (includeRelatedData)
        {
            Query.Include(p => p.Tags)
                 .Include(p => p.PostCategory)
                 .ThenInclude(pc => pc.Category);
        }
    }
}

public sealed class PostByIdForTitleDateSpec : SingleResultSpecification<PostEntity, (string Title, DateTime? PubDateUtc)>
{
    public PostByIdForTitleDateSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
        Query.Select(p => new ValueTuple<string, DateTime?>(p.Title, p.PubDateUtc));
    }
}

public sealed class FeaturedPostSpec : Specification<PostEntity>
{
    public FeaturedPostSpec()
    {
        Query.Where(p => p.IsFeatured && p.PostStatus == PostStatus.Published && !p.IsDeleted);
    }
}

public sealed class PostByDeletionFlagSpec : Specification<PostEntity>
{
    public PostByDeletionFlagSpec(bool isDeleted) => Query.Where(p => p.IsDeleted == isDeleted);
}
