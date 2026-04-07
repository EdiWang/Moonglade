using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public record ListMentionsQuery(
    int PageSize = 10,
    int PageIndex = 1,
    string? Domain = null,
    string? SourceTitle = null,
    string? TargetPostTitle = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<(List<MentionEntity> Mentions, int TotalCount)>;

public class ListMentionsQueryHandler(BlogDbContext db) :
    IQueryHandler<ListMentionsQuery, (List<MentionEntity> Mentions, int TotalCount)>
{
    public async Task<(List<MentionEntity> Mentions, int TotalCount)> HandleAsync(ListMentionsQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        if (request.PageIndex < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageIndex)} can not be less than 1, current value: {request.PageIndex}.");
        }

        IQueryable<MentionEntity> query = db.Mention.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Domain))
            query = query.Where(e => e.Domain.Contains(request.Domain));

        if (!string.IsNullOrWhiteSpace(request.SourceTitle))
            query = query.Where(e => e.SourceTitle.Contains(request.SourceTitle));

        if (!string.IsNullOrWhiteSpace(request.TargetPostTitle))
            query = query.Where(e => e.TargetPostTitle.Contains(request.TargetPostTitle));

        if (request.StartTimeUtc.HasValue)
            query = query.Where(e => e.PingTimeUtc >= request.StartTimeUtc.Value);

        if (request.EndTimeUtc.HasValue)
            query = query.Where(e => e.PingTimeUtc <= request.EndTimeUtc.Value);

        var totalCount = await query.CountAsync(ct);

        var skip = (request.PageIndex - 1) * request.PageSize;

        IOrderedQueryable<MentionEntity> orderedQuery = request.SortBy?.ToLowerInvariant() switch
        {
            "sourceurl" => request.SortDescending
                ? query.OrderByDescending(e => e.SourceUrl)
                : query.OrderBy(e => e.SourceUrl),
            "sourcetitle" => request.SortDescending
                ? query.OrderByDescending(e => e.SourceTitle)
                : query.OrderBy(e => e.SourceTitle),
            "targetposttitle" => request.SortDescending
                ? query.OrderByDescending(e => e.TargetPostTitle)
                : query.OrderBy(e => e.TargetPostTitle),
            _ => request.SortDescending
                ? query.OrderByDescending(e => e.PingTimeUtc)
                : query.OrderBy(e => e.PingTimeUtc)
        };

        var entities = await orderedQuery
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return (entities, totalCount);
    }
}