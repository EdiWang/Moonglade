using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using System.Text.RegularExpressions;

namespace Moonglade.Core.PostFeature;

public record SearchPostQuery(string Keyword) : IRequest<List<PostDigest>>;

public class SearchPostQueryHandler(MoongladeRepository<PostEntity> repo) : IRequestHandler<SearchPostQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> Handle(SearchPostQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentException("Keyword must not be null or whitespace.", nameof(request.Keyword));
        }

        var query = BuildSearchQuery(request.Keyword);
        var results = await query.Select(PostDigest.EntitySelector).ToListAsync(ct);
        return results;
    }

    private IQueryable<PostEntity> BuildSearchQuery(string keyword)
    {
        var normalized = Regex.Replace(keyword.Trim(), @"\s+", " ");
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = repo.AsQueryable()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatusConstants.Published)
            .AsNoTracking();

        if (words.Length > 1)
        {
            // All words must appear in Title
            foreach (var word in words)
            {
                var temp = word; // Required for EF
                query = query.Where(p => p.Title.Contains(temp));
            }
        }
        else
        {
            var word = words[0];
            query = query.Where(p =>
                p.Title.Contains(word) ||
                p.Tags.Any(t => t.DisplayName.Contains(word))
            );
        }

        return query;
    }
}
