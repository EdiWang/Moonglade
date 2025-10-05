using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public record GetArchiveQuery : IQuery<List<Archive>>;

public class GetArchiveQueryHandler(MoongladeRepository<PostEntity> repo) : IQueryHandler<GetArchiveQuery, List<Archive>>
{
    private readonly Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>> _archiveSelector =
        p => new(p.Key.Year, p.Key.Month, p.Count());

    public async Task<List<Archive>> HandleAsync(GetArchiveQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(new PostByStatusSpec(PostStatus.Published), ct))
        {
            return [];
        }

        var spec = new PostByStatusSpec(PostStatus.Published);
        var list = await repo.SelectAsync(
            post => new(post.PubDateUtc.Value.Year, post.PubDateUtc.Value.Month),
            _archiveSelector, spec);

        return list;
    }
}