using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler : IRequestHandler<GetTagQuery, Tag>
{
    private readonly IRepository<TagEntity> _tagRepo;

    public GetTagQueryHandler(IRepository<TagEntity> tagRepo)
    {
        _tagRepo = tagRepo;
    }

    public Task<Tag> Handle(GetTagQuery request, CancellationToken cancellationToken)
    {
        var tag = _tagRepo.SelectFirstOrDefault(new TagSpec(request.NormalizedName), Tag.EntitySelector);
        return Task.FromResult(tag);
    }
}