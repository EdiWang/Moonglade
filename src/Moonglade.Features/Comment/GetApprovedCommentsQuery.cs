using Ardalis.Specification;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Comment;

public record GetApprovedCommentsQuery(Guid PostId) : IQuery<List<Data.DTO.Comment>>;

public class GetApprovedCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IQueryHandler<GetApprovedCommentsQuery, List<Data.DTO.Comment>>
{
    public async Task<List<Data.DTO.Comment>> HandleAsync(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        var spec = new CommentWithRepliesSpec(request.PostId);
        var dtoSpec = new CommentEntityToCommentSpec();

        var newSpec = spec.WithProjectionOf(dtoSpec);

        var list = await repo.ListAsync(newSpec, ct);
        return list;
    }
}