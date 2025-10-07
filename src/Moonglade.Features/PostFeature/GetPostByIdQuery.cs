using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.PostFeature;

public record GetPostByIdQuery(Guid Id) : IQuery<PostEntity>;

public class GetPostByIdQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<GetPostByIdQuery, PostEntity>
{
    public Task<PostEntity> HandleAsync(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, ct);
        return post;
    }
}