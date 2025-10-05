using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Specifications;

namespace Moonglade.Core.PostFeature;

public enum CountType
{
    Public,
    Category,
    Tag,
    Featured
}

public record CountPostQuery(CountType CountType, Guid? CatId = null, int? TagId = null) : IQuery<int>;

public class CountPostQueryHandler(
    MoongladeRepository<PostEntity> postRepo,
    MoongladeRepository<PostTagEntity> postTagRepo,
    MoongladeRepository<PostCategoryEntity> postCatRepo)
    : IQueryHandler<CountPostQuery, int>
{
    public async Task<int> HandleAsync(CountPostQuery request, CancellationToken ct)
    {
        return request.CountType switch
        {
            CountType.Public => await postRepo.CountAsync(new PostByStatusSpec(PostStatus.Published), ct),

            CountType.Category => request.CatId is Guid catId
                ? await postCatRepo.CountAsync(new PostCategorySpec(catId), ct)
                : throw new InvalidOperationException("CatId must be provided for Category count."),

            CountType.Tag => request.TagId is int tagId
                ? await postTagRepo.CountAsync(
                    p => p.TagId == tagId &&
                         p.Post.PostStatus == PostStatusConstants.Published &&
                         !p.Post.IsDeleted, ct)
                : throw new InvalidOperationException("TagId must be provided for Tag count."),

            CountType.Featured => await postRepo.CountAsync(new FeaturedPostSpec(), ct),

            _ => throw new ArgumentOutOfRangeException(nameof(request.CountType), "Unknown CountType.")
        };
    }
}
