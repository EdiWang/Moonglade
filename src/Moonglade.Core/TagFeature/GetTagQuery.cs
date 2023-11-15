using Moonglade.Data.Spec;

namespace Moonglade.Core.TagFeature;

public record GetTagQuery(string NormalizedName) : IRequest<Tag>;

public class GetTagQueryHandler(IRepository<TagEntity> repo) : IRequestHandler<GetTagQuery, Tag>
{
    public Task<Tag> Handle(GetTagQuery request, CancellationToken ct) =>
        repo.FirstOrDefaultAsync(new TagSpec(request.NormalizedName), Tag.EntitySelector);
}