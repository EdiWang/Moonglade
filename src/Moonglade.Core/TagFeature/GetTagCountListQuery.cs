using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.TagFeature;

public record GetTagCountListQuery : IRequest<List<(TagEntity Tag, int PostCount)>>;

public class GetTagCountListQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagCountListQuery, List<(TagEntity Tag, int PostCount)>>
{
    public Task<List<(TagEntity Tag, int PostCount)>> Handle(GetTagCountListQuery request, CancellationToken ct) =>
        repo.ListAsync(new TagCloudSpec(), ct);
}