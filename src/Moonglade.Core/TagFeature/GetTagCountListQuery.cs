using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

namespace Moonglade.Core.TagFeature;

public class GetTagCountListQuery : IRequest<IReadOnlyList<KeyValuePair<Tag, int>>>
{
}

public class GetTagCountListQueryHandler : IRequestHandler<GetTagCountListQuery, IReadOnlyList<KeyValuePair<Tag, int>>>
{
    private readonly IRepository<TagEntity> _tagRepo;

    public GetTagCountListQueryHandler(IRepository<TagEntity> tagRepo)
    {
        _tagRepo = tagRepo;
    }

    public Task<IReadOnlyList<KeyValuePair<Tag, int>>> Handle(GetTagCountListQuery request, CancellationToken cancellationToken)
    {
        return _tagRepo.SelectAsync(t =>
            new KeyValuePair<Tag, int>(new()
            {
                Id = t.Id,
                DisplayName = t.DisplayName,
                NormalizedName = t.NormalizedName
            }, t.Posts.Count));
    }
}