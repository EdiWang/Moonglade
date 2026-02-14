using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;

namespace Moonglade.ActivityLog;

public record ListActivityLogsQuery(int PageSize = 10, int PageIndex = 1) : IQuery<(List<ActivityLogItem> Logs, int TotalCount)>;

public class ListActivityLogsQueryHandler(IRepositoryBase<ActivityLogEntity> repository)
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

        var totalCount = await repository.CountAsync(ct);

        var skip = (request.PageIndex - 1) * request.PageSize;
        var entities = await repository.ListAsync(ct);

        var logs = entities
            .OrderByDescending(e => e.EventTimeUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(ToDto)
            .ToList();

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
        MetaData = entity.MetaData,
        IpAddress = entity.IpAddress,
        UserAgent = entity.UserAgent
    };
}
