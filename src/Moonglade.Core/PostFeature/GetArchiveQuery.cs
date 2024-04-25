using Moonglade.Data;
using Moonglade.Data.Spec;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public record struct Archive(int Year, int Month, int Count);
public record GetArchiveQuery : IRequest<List<Archive>>;

public class GetArchiveQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<GetArchiveQuery, List<Archive>>
{
    private readonly Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>> _archiveSelector =
        p => new(p.Key.Year, p.Key.Month, p.Count());

    public async Task<List<Archive>> Handle(GetArchiveQuery request, CancellationToken ct)
    {
        if (!await repo.AnyAsync(p => p.IsPublished && !p.IsDeleted, ct))
        {
            return new();
        }

        var spec = new PostByStatusSpec(PostStatus.Published);
        var list = await repo.SelectAsync(
            post => new(post.PubDateUtc.Value.Year, post.PubDateUtc.Value.Month),
            _archiveSelector, spec);

        return list;
    }
}