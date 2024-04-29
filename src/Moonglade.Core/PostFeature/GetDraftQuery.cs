using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<PostEntity>;

public class GetDraftQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<GetDraftQuery, PostEntity>
{
    public Task<PostEntity> Handle(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, ct);
        return post;
    }
}