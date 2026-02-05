using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByDateAndSlugSpec : Specification<PostEntity>
{
    public PostByDateAndSlugSpec(DateTime date, string slug, bool includeRelationData)
    {
        Query.Where(p =>
                    p.Slug == slug &&
                    p.PostStatus == PostStatus.Published &&
                    p.PubDateUtc.Value.Date == date &&
                    !p.IsDeleted);

        if (includeRelationData)
        {
            Query.Include(p => p.Comments)
                 .Include(pt => pt.Tags)
                 .Include(p => p.PostCategory)
                    .ThenInclude(pc => pc.Category);
        }
    }
}
