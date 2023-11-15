using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<Post>;

public class GetDraftQueryHandler(IRepository<PostEntity> repo) : IRequestHandler<GetDraftQuery, Post>
{
    public Task<Post> Handle(GetDraftQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Id);
        var post = repo.FirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}