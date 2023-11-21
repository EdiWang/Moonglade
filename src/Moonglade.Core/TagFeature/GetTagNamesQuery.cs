namespace Moonglade.Core.TagFeature;

public record GetTagNamesQuery : IRequest<IReadOnlyList<string>>;

public class GetTagNamesQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagNamesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetTagNamesQuery request, CancellationToken ct) => repo.SelectAsync(t => t.DisplayName, ct);
}