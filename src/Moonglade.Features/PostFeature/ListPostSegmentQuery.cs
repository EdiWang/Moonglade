using Ardalis.Specification;
using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.PostFeature;

public class ListPostSegmentQuery(PostStatus postStatus, int offset, int pageSize, string keyword = null)
    : IQuery<(List<PostSegment> Posts, int TotalRows)>
{
    public PostStatus PostStatus { get; set; } = postStatus;

    public int Offset { get; set; } = offset;

    public int PageSize { get; set; } = pageSize;

    public string Keyword { get; set; } = keyword;
}

public class ListPostSegmentQueryHandler(MoongladeRepository<PostEntity> repo) :
    IQueryHandler<ListPostSegmentQuery, (List<PostSegment> Posts, int TotalRows)>
{
    public async Task<(List<PostSegment> Posts, int TotalRows)> HandleAsync(ListPostSegmentQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        if (request.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.Offset)} can not be less than 0, current value: {request.Offset}.");
        }

        var spec = new PostPagingByStatusSpec(request.PostStatus, request.Keyword, request.PageSize, request.Offset);
        var dtoSpec = new PostEntityToSegmentSpec();
        var newSpec = spec.WithProjectionOf(dtoSpec);

        var posts = await repo.ListAsync(newSpec, ct);

        var countSpec = new PostPagingByStatusSpec(request.PostStatus, request.Keyword);
        var totalRows = await repo.CountAsync(countSpec, ct);

        return (posts, totalRows);
    }
}