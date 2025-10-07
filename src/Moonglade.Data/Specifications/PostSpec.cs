using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostSpec : Specification<PostEntity>
{
    public PostSpec(Guid id, bool includeRelatedData = true)
    {
        Query.Where(p => p.Id == id);

        if (includeRelatedData)
        {
            Query.Include(p => p.Tags)
                 .Include(p => p.PostCategory)
                 .ThenInclude(pc => pc.Category);
        }
    }
}

public sealed class PostByIdForTitleDateSpec : SingleResultSpecification<PostEntity, (string Title, DateTime? PubDateUtc)>
{
    public PostByIdForTitleDateSpec(Guid id)
    {
        Query.Where(p => p.Id == id);
        Query.Select(p => new ValueTuple<string, DateTime?>(p.Title, p.PubDateUtc));
    }
}

public sealed class FeaturedPostSpec : Specification<PostEntity>
{
    public FeaturedPostSpec()
    {
        Query.Where(p => p.IsFeatured && p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
    }
}

public sealed class PostByDeletionFlagSpec : Specification<PostEntity>
{
    public PostByDeletionFlagSpec(bool isDeleted) => Query.Where(p => p.IsDeleted == isDeleted);
}

public class PostEntityToDigestSpec : Specification<PostEntity, PostDigest>
{
    public PostEntityToDigestSpec()
    {
        Query.Select(p => new()
        {
            Title = p.Title,
            Slug = p.Slug,
            ContentAbstract = p.ContentAbstract,
            PubDateUtc = p.PubDateUtc.GetValueOrDefault(),
            LangCode = p.ContentLanguageCode,
            IsFeatured = p.IsFeatured,
            Tags = p.Tags.Select(pt => new Tag
            {
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            })
        });
    }
}
