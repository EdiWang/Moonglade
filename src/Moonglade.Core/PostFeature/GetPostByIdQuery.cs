using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record GetPostByIdQuery(Guid Id) : IRequest<Post>;

public class GetPostByIdQueryHandler : IRequestHandler<GetPostByIdQuery, Post>
{
    private readonly IRepository<PostEntity> _postRepo;

    public GetPostByIdQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<Post> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
    {
        var spec = new PostSpec(request.Id);
        var post = _postRepo.SelectFirstOrDefaultAsync(spec, Post.EntitySelector);
        return post;
    }
}