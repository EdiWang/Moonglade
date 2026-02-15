using LiteBus.Queries.Abstractions;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.ActivityLog;

public record ListActivityLogsQuery(
    int PageSize = 10,
    int PageIndex = 1,
    EventType[]? EventTypes = null,
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null) : IQuery<(List<ActivityLogItem> Logs, int TotalCount)>;

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

        var eventIds = request.EventTypes?.Select(et => (int)et).ToArray();

        var pagingSpec = new ActivityLogPagingSpec(request.PageSize, request.PageIndex, eventIds, request.StartTimeUtc, request.EndTimeUtc);
        var entities = await repository.ListAsync(pagingSpec, ct);

        var countSpec = new ActivityLogCountSpec(eventIds, request.StartTimeUtc, request.EndTimeUtc);
        var totalCount = await repository.CountAsync(countSpec, ct);

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
