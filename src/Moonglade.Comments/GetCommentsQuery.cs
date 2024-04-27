using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record GetCommentsQuery(int PageSize, int PageIndex) : IRequest<List<CommentDetailedItem>>;

public class GetCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IRequestHandler<GetCommentsQuery, List<CommentDetailedItem>>
{
    public Task<List<CommentDetailedItem>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentPagingSepc(request.PageSize, request.PageIndex);
        var comments = repo.SelectAsync(spec, CommentDetailedItem.EntitySelector, ct);

        return comments;
    }
}