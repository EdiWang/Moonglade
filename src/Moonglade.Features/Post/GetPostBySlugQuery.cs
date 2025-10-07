using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public record GetPostBySlugQuery(string RouteLink) : IQuery<PostEntity>;

public class GetPostBySlugQueryHandler(MoongladeRepository<PostEntity> repo)
    : IQueryHandler<GetPostBySlugQuery, PostEntity>
{
    public async Task<PostEntity> HandleAsync(GetPostBySlugQuery request, CancellationToken ct)
    {
        var spec = new PostByRouteLinkSpec(request.RouteLink);

        var post = await repo.FirstOrDefaultAsync(spec, ct);
        return post;
    }
}