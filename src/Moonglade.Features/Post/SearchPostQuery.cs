using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using System.Text.RegularExpressions;

namespace Moonglade.Features.Post;

public enum SearchPostSort
{
    Newest = 0,
    Oldest = 1,
    TitleAscending = 2,
    TitleDescending = 3
}

public record SearchPostQuery(
    string Keyword,
    int PageSize = 10,
    int PageIndex = 1,
    string CategorySlug = null,
    string Tag = null,
    string LanguageCode = null,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null,
    SearchPostSort Sort = SearchPostSort.Newest) : IQuery<SearchPostQueryResult>;

public record SearchPostQueryResult(List<PostDigest> Posts, int TotalRows);

public class SearchPostQueryHandler(BlogDbContext db) : IQueryHandler<SearchPostQuery, SearchPostQueryResult>
{
    public async Task<SearchPostQueryResult> HandleAsync(SearchPostQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentException("Keyword must not be null or whitespace.", nameof(request.Keyword));
        }

        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageSize), "Page size must be greater than 0.");
        }

        if (request.PageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request.PageIndex), "Page index must be greater than 0.");
        }

        var words = NormalizeKeyword(request.Keyword);

        IQueryable<PostEntity> query = db.Post
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Published);

        query = ApplyKeywordFilter(query, words);
        query = ApplyFacetFilters(query, request);

        var totalRows = await query.CountAsync(ct);
        var posts = await ApplySorting(query, request.Sort)
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .SelectToDigest()
            .ToListAsync(ct);

        return new(posts, totalRows);
    }

    private static string[] NormalizeKeyword(string keyword)
    {
        var normalized = Regex.Replace(keyword.Trim(), @"\s+", " ");
        return normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static IQueryable<PostEntity> ApplyKeywordFilter(IQueryable<PostEntity> query, IEnumerable<string> words)
    {
        foreach (var word in words)
        {
            var keyword = word;
            query = query.Where(p =>
                p.Title.Contains(keyword) ||
                (p.ContentAbstract != null && p.ContentAbstract.Contains(keyword)) ||
                (p.Keywords != null && p.Keywords.Contains(keyword)) ||
                p.Tags.Any(t => t.DisplayName.Contains(keyword) || t.NormalizedName.Contains(keyword)) ||
                p.PostCategory.Any(pc => pc.Category != null && (pc.Category.DisplayName.Contains(keyword) || pc.Category.Slug.Contains(keyword))));
        }

        return query;
    }

    private static IQueryable<PostEntity> ApplyFacetFilters(IQueryable<PostEntity> query, SearchPostQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var categorySlug = request.CategorySlug.Trim().ToLower();
            query = query.Where(p => p.PostCategory.Any(pc => pc.Category.Slug == categorySlug));
        }

        if (!string.IsNullOrWhiteSpace(request.Tag))
        {
            var tag = request.Tag.Trim().ToLower();
            query = query.Where(p => p.Tags.Any(t => t.NormalizedName == tag));
        }

        if (!string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            var languageCode = request.LanguageCode.Trim().ToLower();
            query = query.Where(p => p.ContentLanguageCode == languageCode);
        }

        if (request.StartDateUtc.HasValue)
        {
            var startDateUtc = request.StartDateUtc.Value.Date;
            query = query.Where(p => p.PubDateUtc >= startDateUtc);
        }

        if (request.EndDateUtc.HasValue)
        {
            var endDateUtc = request.EndDateUtc.Value.Date.AddDays(1);
            query = query.Where(p => p.PubDateUtc < endDateUtc);
        }

        return query;
    }

    private static IOrderedQueryable<PostEntity> ApplySorting(IQueryable<PostEntity> query, SearchPostSort sort) =>
        sort switch
        {
            SearchPostSort.Oldest => query
                .OrderBy(p => p.PubDateUtc)
                .ThenBy(p => p.Title),
            SearchPostSort.TitleAscending => query
                .OrderBy(p => p.Title)
                .ThenByDescending(p => p.PubDateUtc),
            SearchPostSort.TitleDescending => query
                .OrderByDescending(p => p.Title)
                .ThenByDescending(p => p.PubDateUtc),
            _ => query
                .OrderByDescending(p => p.PubDateUtc)
                .ThenBy(p => p.Title)
        };
}
