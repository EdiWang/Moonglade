using Ardalis.Specification;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record ListCommentsQuery(int PageSize, int PageIndex, string SearchTerm = null) : IQuery<List<CommentDetailedItem>>;

public class ListCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<ListCommentsQuery, List<CommentDetailedItem>>
{
    public Task<List<CommentDetailedItem>> HandleAsync(ListCommentsQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        var spec = new CommentPagingSepc(request.PageSize, request.PageIndex, request.SearchTerm);
        var dtoSpec = new CommentEntityToCommentDetailedItemSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        var comments = repo.ListAsync(newSpec, ct);

        return comments;
    }
}