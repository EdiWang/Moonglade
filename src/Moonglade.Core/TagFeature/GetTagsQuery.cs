namespace Moonglade.Core.TagFeature;

public record GetTagsQuery : IRequest<IReadOnlyList<Tag>>;

public class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<Tag>>
{
    private readonly IRepository<TagEntity> _tagRepo;


    public GetTagsQueryHandler(IRepository<TagEntity> tagRepo)
    {
        _tagRepo = tagRepo;
    }

    public Task<IReadOnlyList<Tag>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        return _tagRepo.SelectAsync(Tag.EntitySelector);
    }
}