using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostByRouteLinkForIdTitleSpec : SingleResultSpecification<PostEntity, (Guid Id, string Title)>
{
    public PostByRouteLinkForIdTitleSpec(string routeLink)
    {
        Query.Where(p =>
            p.RouteLink == routeLink &&
            p.PostStatus == PostStatusConstants.Published &&
            !p.IsDeleted);

        Query.Select(p => new ValueTuple<Guid, string>(p.Id, p.Title));
    }
}
