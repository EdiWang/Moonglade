using Ardalis.Specification;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Comments;

public record GetApprovedCommentsQuery(Guid PostId) : IQuery<List<Comment>>;

public class GetApprovedCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<GetApprovedCommentsQuery, List<Comment>>
{
    public async Task<List<Comment>> HandleAsync(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentWithRepliesSpec(request.PostId);
        var dtoSpec = new CommentEntityToCommentSpec();

        var newSpec = spec.WithProjectionOf(dtoSpec);

        var list = await repo.ListAsync(newSpec, ct);
        return list;
    }
}