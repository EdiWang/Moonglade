using LiteBus.Queries.Abstractions;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.ActivityLog;

public record ListActivityLogsQuery(
    int PageSize = 10,
    int PageIndex = 1,
    EventType[]? EventTypes = null,
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

        var eventIds = request.EventTypes?.Select(et => (int)et).ToArray();

        IQueryable<ActivityLogEntity> query = db.ActivityLog.AsNoTracking();

        if (eventIds is { Length: > 0 })
        {
            query = query.Where(e => eventIds.Contains(e.EventId));
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
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var logs = entities.Select(ToDto).ToList();

        return (logs, totalCount);
    }

    private static ActivityLogItem ToDto(ActivityLogEntity entity) => new()
    {
        Id = entity.Id,
        EventType = (EventType)entity.EventId,
        EventTimeUtc = entity.EventTimeUtc ?? DateTime.MinValue,
        ActorId = entity.ActorId,
        Operation = entity.Operation,
        TargetName = entity.TargetName,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent
    };
}
