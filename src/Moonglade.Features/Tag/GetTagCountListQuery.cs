using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Tag;

public record GetTagCountListQuery : IQuery<List<TagWithCount>>;

public class GetTagCountListQueryHandler(IRepositoryBase<TagEntity> repo) : IQueryHandler<GetTagCountListQuery, List<TagWithCount>>
{
    public Task<List<TagWithCount>> HandleAsync(GetTagCountListQuery request, CancellationToken ct) =>
        repo.ListAsync(new TagCloudSpec(), ct);
}