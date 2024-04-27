using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PostSitePageSpec : Specification<PostEntity>
{
    public PostSitePageSpec()
    {
        Query.Where(p => p.IsPublished && !p.IsDeleted);
        Query.AsNoTracking();
    }
}