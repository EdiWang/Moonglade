using Moonglade.Data.Spec;
using System.Linq.Expressions;

namespace Moonglade.Core.PostFeature;

public record struct Archive(int Year, int Month, int Count);
public record GetArchiveQuery : IRequest<IReadOnlyList<Archive>>;

public class GetArchiveQueryHandler : IRequestHandler<GetArchiveQuery, IReadOnlyList<Archive>>
{
    private readonly IRepository<PostEntity> _repo;
    private readonly Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>> _archiveSelector =
        p => new(p.Key.Year, p.Key.Month, p.Count());

    public GetArchiveQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public async Task<IReadOnlyList<Archive>> Handle(GetArchiveQuery request, CancellationToken ct)
    {
        if (!await _repo.AnyAsync(p => p.IsPublished && !p.IsDeleted, ct))
        {
            return new List<Archive>();
        }

        var spec = new PostSpec(PostStatus.Published);
        var list = await _repo.SelectAsync(
            post => new(post.PubDateUtc.Value.Year, post.PubDateUtc.Value.Month),
            _archiveSelector, spec);

        return list;
    }
}