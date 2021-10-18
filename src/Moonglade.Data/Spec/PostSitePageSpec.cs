using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Data.Spec;

public class PostSitePageSpec : BaseSpecification<PostEntity>
{
    public PostSitePageSpec() : base(p =>
        p.IsPublished && !p.IsDeleted && p.ExposedToSiteMap)
    {

    }
}