using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core.PostFeature;

public class ListArchiveQuery : IRequest<IReadOnlyList<PostDigest>>
{
    public ListArchiveQuery(int year, int? month = null)
    {
        Year = year;
        Month = month;
    }

    public int Year { get; set; }
    public int? Month { get; set; }
}

public class ListArchiveQueryHandler : IRequestHandler<ListArchiveQuery, IReadOnlyList<PostDigest>>
{
    private readonly IRepository<PostEntity> _postRepo;

    public ListArchiveQueryHandler(IRepository<PostEntity> postRepo)
    {
        _postRepo = postRepo;
    }

    public Task<IReadOnlyList<PostDigest>> Handle(ListArchiveQuery request, CancellationToken cancellationToken)
    {
        if (request.Year < DateTime.MinValue.Year || request.Year > DateTime.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Year));
        }

        if (request.Month is > 12 or < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Month));
        }

        var spec = new PostSpec(request.Year, request.Month.GetValueOrDefault());
        var list = _postRepo.SelectAsync(spec, PostDigest.EntitySelector);
        return list;
    }
}