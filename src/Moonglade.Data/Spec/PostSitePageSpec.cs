using Moonglade.Data.Entities;

namespace Moonglade.Data.Spec;

public class PostSitePageSpec : Specification<PostEntity>
{
    public PostSitePageSpec()
    {
        Query.Where(p =>
            p.IsPublished && !p.IsDeleted);
    }
}