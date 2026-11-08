using LiteBus.Queries.Abstractions;
using Moonglade.Data;

namespace Moonglade.ActivityLog;

public record GetMetaDataByActivityLogIdQuery(long ActivityLogId) : IQuery<ActivityLogItem?>;

public class GetMetaDataByActivityLogIdQueryHandler(BlogDbContext db)
    : IQueryHandler<GetMetaDataByActivityLogIdQuery, ActivityLogItem?>
{
    public async Task<ActivityLogItem?> HandleAsync(GetMetaDataByActivityLogIdQuery request, CancellationToken ct)
    {
        if (request.ActivityLogId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.ActivityLogId)} must be greater than 0, current value: {request.ActivityLogId}.");
        }

        var entity = await db.ActivityLog.AsNoTracking().SingleOrDefaultAsync(e => e.Id == request.ActivityLogId, ct);

        return entity is null ? null : ActivityLogItem.FromEntity(entity);
    }
}
