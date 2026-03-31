using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using System.Text.RegularExpressions;

namespace Moonglade.Features.Post;

public record SearchPostQuery(string Keyword) : IQuery<List<PostDigest>>;

public class SearchPostQueryHandler(BlogDbContext db) : IQueryHandler<SearchPostQuery, List<PostDigest>>
{
    public async Task<List<PostDigest>> HandleAsync(SearchPostQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Keyword))
        {
            throw new ArgumentException("Keyword must not be null or whitespace.", nameof(request.Keyword));
        }

        var normalized = Regex.Replace(request.Keyword.Trim(), @"\s+", " ");
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        IQueryable<PostEntity> query = db.Post
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.PostStatus == PostStatus.Published);

        if (words.Length > 1)
        {
            foreach (var word in words)
            {
                query = query.Where(p => EF.Functions.Like(p.Title, "%" + word + "%"));
            }
        }
        else
        {
            var word = words[0];
            query = query.Where(p =>
                p.Title.Contains(word) ||
                p.Tags.Any(t => t.DisplayName.Contains(word)));
        }

        var results = await query
            .SelectToDigest()
            .ToListAsync(ct);

        return results;
    }
}
