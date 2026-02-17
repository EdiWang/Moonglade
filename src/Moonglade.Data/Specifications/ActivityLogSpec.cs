using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class ActivityLogPagingSpec : Specification<ActivityLogEntity>
{
    public ActivityLogPagingSpec(int pageSize, int pageIndex, int[]? eventIds = null, DateTime? startTimeUtc = null, DateTime? endTimeUtc = null)
    {
        if (eventIds is { Length: > 0 })
        {
            Query.Where(e => eventIds.Contains(e.EventId));
        }

        if (startTimeUtc.HasValue)
        {
            Query.Where(e => e.EventTimeUtc >= startTimeUtc.Value);
        }

        if (endTimeUtc.HasValue)
        {
            Query.Where(e => e.EventTimeUtc <= endTimeUtc.Value);
        }

        var skip = (pageIndex - 1) * pageSize;

        Query.OrderByDescending(e => e.EventTimeUtc);
        Query.Skip(skip).Take(pageSize);
    }
}

public sealed class ActivityLogCountSpec : Specification<ActivityLogEntity>
{
    public ActivityLogCountSpec(int[]? eventIds = null, DateTime? startTimeUtc = null, DateTime? endTimeUtc = null)
    {
        if (eventIds is { Length: > 0 })
        {
            Query.Where(e => eventIds.Contains(e.EventId));
        }

        if (startTimeUtc.HasValue)
        {
            Query.Where(e => e.EventTimeUtc >= startTimeUtc.Value);
        }

        if (endTimeUtc.HasValue)
        {
            Query.Where(e => e.EventTimeUtc <= endTimeUtc.Value);
        }
    }
}
