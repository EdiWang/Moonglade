using Moonglade.Data;

namespace Moonglade.Core.TagFeature;

public record GetTagNamesQuery : IRequest<List<string>>;

public class GetTagNamesQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagNamesQuery, List<string>>
{
    public Task<List<string>> Handle(GetTagNamesQuery request, CancellationToken ct) => repo.SelectAsync(t => t.DisplayName, ct);
}