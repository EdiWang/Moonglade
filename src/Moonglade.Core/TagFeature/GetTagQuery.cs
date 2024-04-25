using Moonglade.Data;
using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler(MoongladeRepository<TagEntity> repo) : IRequestHandler<GetTagQuery, Tag>
{
    public Task<Tag> Handle(GetTagQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new TagByNormalizedNameSpec(request.NormalizedName), Tag.EntitySelector);
}