using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<Post>;

public class GetDraftQueryHandler : IRequestHandler<GetDraftQuery, Post>
{
    private readonly IRepository<PostEntity> _repo;

    public GetDraftQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<Post> Handle(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = _repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}