using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;

public class GetPostByIdQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<GetPostByIdQuery, Post>
{
    public Task<Post> Handle(GetPostByIdQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}