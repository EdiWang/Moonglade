using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.ActivityLog;

public record ListActivityLogsQuery(
    int PageSize = 10,
    int PageIndex = 1,
    string[]? EventKeys = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null) : IQuery<(List<ActivityLogItem> Logs, int TotalCount)>;

public class ListActivityLogsQueryHandler(BlogDbContext db)
    : IQueryHandler<ListActivityLogsQuery, (List<ActivityLogItem> Logs, int TotalCount)>
{
    public async Task<(List<ActivityLogItem> Logs, int TotalCount)> HandleAsync(ListActivityLogsQuery request, CancellationToken ct)
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

        if (request.EventKeys?.Any(key => !ActivityEvents.IsSupported(key)) == true)
        {
            throw new ArgumentException("Unsupported activity event filter.", nameof(request));
        }

        IQueryable<ActivityLogEntity> query = db.ActivityLog.AsNoTracking();

        if (request.EventKeys is { Length: > 0 })
        {
            query = query.Where(e => request.EventKeys.Contains(e.EventKey));
        }

        if (request.StartTimeUtc.HasValue)
        {
            query = query.Where(e => e.EventTimeUtc >= request.StartTimeUtc.Value);
        }

        if (request.EndTimeUtc.HasValue)
        {
            query = query.Where(e => e.EventTimeUtc <= request.EndTimeUtc.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var skip = (request.PageIndex - 1) * request.PageSize;
        var entities = await query
            .OrderByDescending(e => e.EventTimeUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var logs = entities.Select(ActivityLogItem.FromEntity).ToList();

        return (logs, totalCount);
    }

}
