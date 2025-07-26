using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record ListCommentsQuery(int PageSize, int PageIndex) : IQuery<List<CommentDetailedItem>>;

public class ListCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<ListCommentsQuery, List<CommentDetailedItem>>
{
    public Task<List<CommentDetailedItem>> HandleAsync(ListCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentPagingSepc(request.PageSize, request.PageIndex);
        var comments = repo.SelectAsync(spec, CommentDetailedItem.EntitySelector, ct);

        return comments;
    }
}