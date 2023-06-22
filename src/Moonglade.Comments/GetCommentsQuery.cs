using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record GetCommentsQuery(int PageSize, int PageIndex) : IRequest<IReadOnlyList<CommentDetailedItem>>;

public class GetCommentsQueryHandler : IRequestHandler<GetCommentsQuery, IReadOnlyList<CommentDetailedItem>>
{
    private readonly IRepository<CommentEntity> _repo;
    public GetCommentsQueryHandler(IRepository<CommentEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<CommentDetailedItem>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentSpec(request.PageSize, request.PageIndex);
        var comments = _repo.SelectAsync(spec, CommentDetailedItem.EntitySelector, ct);

        return comments;
    }
}