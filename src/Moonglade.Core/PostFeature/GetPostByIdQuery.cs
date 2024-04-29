using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<PostEntity>;

public class GetPostByIdQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<GetPostByIdQuery, PostEntity>
{
    public Task<PostEntity> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, ct);
        return post;
    }
}