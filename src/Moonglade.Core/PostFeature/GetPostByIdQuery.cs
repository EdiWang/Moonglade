using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, Post>
{
    private readonly IRepository<PostEntity> _repo;

    public GetPostByIdQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<Post> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = _repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}