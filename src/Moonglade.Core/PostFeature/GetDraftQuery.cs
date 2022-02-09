using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetDraftQuery(Guid Id) : IRequest<Post>;

public class GetDraftQueryHandler : IRequestHandler<GetDraftQuery, Post>
{
    private readonly IRepository<PostEntity> _postRepo;

    public GetDraftQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<Post> Handle(GetDraftQuery request, CancellationToken cancellationToken)
    {
        var spec = new PostSpec(request.Id);
        var post = _postRepo.SelectFirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}