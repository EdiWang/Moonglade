using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler : RequestHandler<GetTagQuery, Tag>
{
    private readonly IRepository<TagEntity> _tagRepo;

    public GetTagQueryHandler(IRepository<TagEntity> tagRepo) => _tagRepo = tagRepo;

    protected override Tag Handle(GetTagQuery request) => _tagRepo.SelectFirstOrDefault(new TagSpec(request.NormalizedName), Tag.EntitySelector);
}