using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler : RequestHandler<GetTagQuery, Tag>
{
    private readonly IRepository<TagEntity> _repo;

    public GetTagQueryHandler(IRepository<TagEntity> repo) => _repo = repo;

    protected override Tag Handle(GetTagQuery request) => _repo.SelectFirstOrDefault(new TagSpec(request.NormalizedName), Tag.EntitySelector);
}