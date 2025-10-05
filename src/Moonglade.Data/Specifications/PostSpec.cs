using Moonglade.Data.DTO;
using Moonglade.Data.Entities;
using System.Text.RegularExpressions;

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

public sealed class PostByStatusSpec : Specification<PostEntity>
{
    public PostByStatusSpec(PostStatus status)
    {
        switch (status)
        {
            case PostStatus.Draft:
                Query.Where(p => p.PostStatus == PostStatusConstants.Draft && !p.IsDeleted);
                break;
            case PostStatus.Scheduled:
                Query.Where(p => p.PostStatus == PostStatusConstants.Scheduled && !p.IsDeleted);
                break;
            case PostStatus.Published:
                Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
                break;
            case PostStatus.Deleted:
                Query.Where(p => p.IsDeleted);
                break;
            case PostStatus.Default:
                Query.Where(p => true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        Query.AsNoTracking();
    }
}

public sealed class FeaturedPostSpec : Specification<PostEntity>
{
    public FeaturedPostSpec()
    {
        Query.Where(p => p.IsFeatured && p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);
    }
}

public sealed class PostByCatSpec : Specification<PostEntity>
{
    public PostByCatSpec(Guid? categoryId, int? top = null)
    {
        Query.Where(p =>
                    !p.IsDeleted &&
                    p.PostStatus == PostStatusConstants.Published &&
                    p.IsFeedIncluded &&
                    (categoryId == null || p.PostCategory.Any(c => c.CategoryId == categoryId.Value)));

        Query.OrderByDescending(p => p.PubDateUtc);

        if (top.HasValue)
        {
            Query.Skip(0).Take(top.Value);
        }
    }
}

public sealed class PostByYearMonthSpec : Specification<PostEntity>
{
    public PostByYearMonthSpec(int year, int month = 0)
    {
        Query.Where(p => p.PubDateUtc.Value.Year == year &&
                         (month == 0 || p.PubDateUtc.Value.Month == month));

        // Fix #313: Filter out unpublished posts
        Query.Where(p => p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);

        Query.OrderByDescending(p => p.PubDateUtc);
        Query.AsNoTracking();
    }
}

public sealed class PostByDeletionFlagSpec : Specification<PostEntity>
{
    public PostByDeletionFlagSpec(bool isDeleted) => Query.Where(p => p.IsDeleted == isDeleted);
}

public sealed class PostByRouteLinkSpec : SingleResultSpecification<PostEntity>
{
    public PostByRouteLinkSpec(string routeLink)
    {
        Query.Where(p => p.RouteLink == routeLink && p.PostStatus == PostStatusConstants.Published && !p.IsDeleted);

        Query.Include(p => p.Comments)
             .Include(pt => pt.Tags)
             .Include(p => p.PostCategory)
                .ThenInclude(pc => pc.Category)
             .AsSplitQuery();
    }
}

public sealed class PostByDateAndSlugSpec : Specification<PostEntity>
{
    public PostByDateAndSlugSpec(DateTime date, string slug, bool includeRelationData)
    {
        Query.Where(p =>
                    p.Slug == slug &&
                    p.PostStatus == PostStatusConstants.Published &&
                    p.PubDateUtc.Value.Date == date &&
                    !p.IsDeleted);

        if (includeRelationData)
        {
            Query.Include(p => p.Comments)
                 .Include(pt => pt.Tags)
                 .Include(p => p.PostCategory)
                    .ThenInclude(pc => pc.Category);
        }
    }
}

public sealed class PostByRouteLinkForIdTitleSpec : SingleResultSpecification<PostEntity, (Guid Id, string Title)>
{
    public PostByRouteLinkForIdTitleSpec(string routeLink)
    {
        Query.Where(p =>
            p.RouteLink == routeLink &&
            p.PostStatus == PostStatusConstants.Published &&
            !p.IsDeleted);

        Query.Select(p => new ValueTuple<Guid, string>(p.Id, p.Title));
    }
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
            Author = "", // To be filled by caller
            AuthorEmail = "", // To be filled by caller
            LangCode = p.ContentLanguageCode,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToArray()
        });
    }
}

public class PostEntityToPostExportModelSpec : Specification<PostEntity, PostExportModel>
{
    public PostEntityToPostExportModelSpec()
    {
        Query.Select(p => new PostExportModel
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            RouteLink = p.RouteLink,
            Author = p.Author,
            ContentAbstract = p.ContentAbstract,
            PostContent = p.PostContent,
            HeroImageUrl = p.HeroImageUrl,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ScheduledPublishTimeUtc = p.ScheduledPublishTimeUtc,
            CommentEnabled = p.CommentEnabled,
            PubDateUtc = p.PubDateUtc,
            ContentLanguageCode = p.ContentLanguageCode,
            IsDeleted = p.IsDeleted,
            IsFeedIncluded = p.IsFeedIncluded,
            IsFeatured = p.IsFeatured,
            PostStatus = p.PostStatus,
            IsOutdated = p.IsOutdated,
            Keywords = p.Keywords,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList(),
            Tags = p.Tags.Select(t => t.DisplayName).ToList()
        });
    }
}