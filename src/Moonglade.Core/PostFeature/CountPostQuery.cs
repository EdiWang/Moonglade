namespace Moonglade.Core.PostFeature;

public enum CountType
{
    Public,
    Category,
    Tag,
    Featured
}

public record CountPostQuery(CountType CountType, Guid? CatId = null, int? TagId = null) : IRequest<int>;

public class CountPostQueryHandler : IRequestHandler<CountPostQuery, int>
{
    private readonly IRepository<PostEntity> _postRepo;
    private readonly IRepository<PostTagEntity> _postTagRepo;
    private readonly IRepository<PostCategoryEntity> _postCatRepo;

    public CountPostQueryHandler(
        IRepository<PostEntity> postRepo,
        IRepository<PostTagEntity> postTagRepo,
        IRepository<PostCategoryEntity> postCatRepo)
    {
        _postRepo = postRepo;
        _postTagRepo = postTagRepo;
        _postCatRepo = postCatRepo;
    }

    public async Task<int> Handle(CountPostQuery request, CancellationToken ct)
    {
        int count = 0;

        switch (request.CountType)
        {
            case CountType.Public:
                count = await _postRepo.CountAsync(p => p.IsPublished && !p.IsDeleted, ct);
                break;

            case CountType.Category:
                if (request.CatId == null) throw new ArgumentNullException(nameof(request.CatId));
                count = await _postCatRepo.CountAsync(c => c.CategoryId == request.CatId.Value
                                                           && c.Post.IsPublished
                                                           && !c.Post.IsDeleted, ct);
                break;

            case CountType.Tag:
                if (request.TagId == null) throw new ArgumentNullException(nameof(request.TagId));
                count = await _postTagRepo.CountAsync(p => p.TagId == request.TagId.Value && p.Post.IsPublished && !p.Post.IsDeleted, ct);
                break;

            case CountType.Featured:
                count = await _postRepo.CountAsync(p => p.IsFeatured && p.IsPublished && !p.IsDeleted, ct);
                break;
        }

        return count;
    }
}