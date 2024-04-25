using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class PostSitePageSpec : Specification<PostEntity>
{
    public PostSitePageSpec()
    {
        Query.Where(p =>
            p.IsPublished && !p.IsDeleted);
    }
}