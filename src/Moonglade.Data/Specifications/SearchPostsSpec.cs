using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using System.Text.RegularExpressions;

namespace Moonglade.Data.Specifications;

public class SearchPostsSpec : Specification<PostEntity, PostDigest>
{
    public SearchPostsSpec(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Keyword must not be null or whitespace.", nameof(keyword));
        }

        var normalized = Regex.Replace(keyword.Trim(), @"\s+", " ");
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Query.Where(p => !p.IsDeleted && p.PostStatus == PostStatusConstants.Published);

        if (words.Length > 1)
        {
            // All words must appear in Title
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                Query.Search(p => p.Title, "%" + word + "%", group: i);
            }
        }
        else
        {
            var word = words[0];
            Query.Where(p =>
                p.Title.Contains(word) ||
                p.Tags.Any(t => t.DisplayName.Contains(word))
            );
        }

        Query.Select(p => new PostDigest
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
