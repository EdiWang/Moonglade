using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class PostTagSpec : Specification<PostTagEntity>
{
    public PostTagSpec(int tagId, int pageSize, int pageIndex)
    {
        Query.Where(pt =>
            pt.TagId == tagId
            && !pt.Post.IsDeleted
            && pt.Post.PostStatus == PostStatusConstants.Published);

        var startRow = (pageIndex - 1) * pageSize;
        Query.Skip(startRow).Take(pageSize);
        Query.OrderByDescending(p => p.Post.PubDateUtc);
    }
}

public sealed class PostTagByTagIdSpec : SingleResultSpecification<PostTagEntity>
{
    public PostTagByTagIdSpec(int tagId)
    {
        Query.Where(pt => pt.TagId == tagId);
    }
}

public class PostTagEntityToPostDigestSpec : Specification<PostTagEntity, PostDigest>
{
    public PostTagEntityToPostDigestSpec()
    {
        Query.Select(pt => new PostDigest
        {
            Title = pt.Post.Title,
            Slug = pt.Post.Slug,
            ContentAbstract = pt.Post.ContentAbstract,
            PubDateUtc = pt.Post.PubDateUtc.GetValueOrDefault(),
            LangCode = pt.Post.ContentLanguageCode,
            IsFeatured = pt.Post.IsFeatured,
            Tags = pt.Post.Tags.Select(tag => new Tag
            {
                NormalizedName = tag.NormalizedName,
                DisplayName = tag.DisplayName
            })
        });
    }
}