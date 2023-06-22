using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public record ListArchiveQuery(int Year, int? Month = null) : IRequest<IReadOnlyList<PostDigest>>;

public class ListArchiveQueryHandler : IRequestHandler<ListArchiveQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _repo;
    public ListArchiveQueryHandler(IRepository<PostEntity> repo) => _repo = repo;

    public Task<IReadOnlyList<PostDigest>> Handle(ListArchiveQuery request, CancellationToken ct)
    {
        var spec = new PostSpec(request.Year, request.Month.GetValueOrDefault());
        var list = _repo.SelectAsync(spec, PostDigest.EntitySelector, ct);
        return list;
    }
}