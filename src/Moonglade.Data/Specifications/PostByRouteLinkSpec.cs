using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByRouteLinkSpec : SingleResultSpecification<PostEntity>
{
    public PostByRouteLinkSpec(string routeLink)
    {
        Query.Where(p => p.RouteLink == routeLink && p.PostStatus == PostStatus.Published && !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
             .AsSplitQuery();
    }
}
