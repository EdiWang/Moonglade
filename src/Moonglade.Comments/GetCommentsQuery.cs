using LiteBus.Queries.Abstractions;
using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record GetCommentsQuery(int PageSize, int PageIndex) : IQuery<List<CommentDetailedItem>>;

public class GetCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<GetCommentsQuery, List<CommentDetailedItem>>
{
    public Task<List<CommentDetailedItem>> HandleAsync(GetCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentPagingSepc(request.PageSize, request.PageIndex);
        var comments = repo.SelectAsync(spec, CommentDetailedItem.EntitySelector, ct);

        return comments;
    }
}