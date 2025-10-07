using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PostEntityToFeedEntrySpec : Specification<PostEntity, FeedEntry>
{
    public PostEntityToFeedEntrySpec(string baseUrl)
    {
        Query.Where(p => p.PubDateUtc != null);
        Query.Select(p => new FeedEntry
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            PubDateUtc = p.PubDateUtc.Value,
            Description = p.ContentAbstract,
            Link = $"{baseUrl}/post/{p.RouteLink}",
            Author = p.Author,
            LangCode = p.ContentLanguageCode,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
        });
    }
}
