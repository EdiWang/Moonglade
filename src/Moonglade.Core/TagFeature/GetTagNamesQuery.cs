namespace Moonglade.Core.TagFeature;

public record GetTagNamesQuery : IRequest<List<string>>;

public class GetTagNamesQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagNamesQuery, List<string>>
{
    public Task<List<string>> Handle(GetTagNamesQuery request, CancellationToken ct) => repo.SelectAsync(t => t.DisplayName, ct);
}