using LiteBus.Queries.Abstractions;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Post;

public enum CountType
{
    Public,
    Category,
    Tag,
    Featured
}

public record CountPostQuery(CountType CountType, Guid? CatId = null, int? TagId = null) : IQuery<int>;

public class CountPostQueryHandler(
    IRepositoryBase<PostEntity> postRepo,
    BlogDbContext db)
    : IQueryHandler<CountPostQuery, int>
{
    public async Task<int> HandleAsync(CountPostQuery request, CancellationToken ct)
    {
        return request.CountType switch
        {
            CountType.Public => await postRepo.CountAsync(new PostByStatusSpec(PostStatus.Published), ct),

            CountType.Category => request.CatId is Guid catId
                ? await db.PostCategory.CountAsync(
                    pc => pc.CategoryId == catId
                          && pc.Post.PostStatus == PostStatus.Published
                          && !pc.Post.IsDeleted, ct)
                : throw new InvalidOperationException("CatId must be provided for Category count."),

            CountType.Tag => request.TagId is int tagId
                ? await db.PostTag.CountAsync(
                    pt => pt.TagId == tagId
                          && pt.Post.PostStatus == PostStatus.Published
                          && !pt.Post.IsDeleted, ct)
                : throw new InvalidOperationException("TagId must be provided for Tag count."),

            CountType.Featured => await db.Post.CountAsync(p => p.IsFeatured && p.PostStatus == PostStatus.Published && !p.IsDeleted, ct),

            _ => throw new ArgumentOutOfRangeException(nameof(request.CountType), "Unknown CountType.")
        };
    }
}
