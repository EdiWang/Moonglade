using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public sealed class MentionPagingSpec : Specification<MentionEntity>
{
    public MentionPagingSpec(
        int pageSize,
        int pageIndex,
        string? domain = null,
        string? sourceTitle = null,
        string? targetPostTitle = null,
        DateTime? startTimeUtc = null,
        DateTime? endTimeUtc = null)
    {
        ApplyFilters(domain, sourceTitle, targetPostTitle, startTimeUtc, endTimeUtc);

        var skip = (pageIndex - 1) * pageSize;

        Query.OrderByDescending(e => e.PingTimeUtc);
        Query.Skip(skip).Take(pageSize);
    }

    private void ApplyFilters(string? domain, string? sourceTitle, string? targetPostTitle, DateTime? startTimeUtc, DateTime? endTimeUtc)
    {
        if (!string.IsNullOrWhiteSpace(domain))
        {
            Query.Where(e => e.Domain.Contains(domain));
        }

        if (!string.IsNullOrWhiteSpace(sourceTitle))
        {
            Query.Where(e => e.SourceTitle.Contains(sourceTitle));
        }

        if (!string.IsNullOrWhiteSpace(targetPostTitle))
        {
            Query.Where(e => e.TargetPostTitle.Contains(targetPostTitle));
        }

        if (startTimeUtc.HasValue)
        {
            Query.Where(e => e.PingTimeUtc >= startTimeUtc.Value);
        }

        if (endTimeUtc.HasValue)
        {
            Query.Where(e => e.PingTimeUtc <= endTimeUtc.Value);
        }
    }
}

public sealed class MentionCountSpec : Specification<MentionEntity>
{
    public MentionCountSpec(
        string? domain = null,
        string? sourceTitle = null,
        string? targetPostTitle = null,
        DateTime? startTimeUtc = null,
        DateTime? endTimeUtc = null)
    {
        if (!string.IsNullOrWhiteSpace(domain))
        {
            Query.Where(e => e.Domain.Contains(domain));
        }

        if (!string.IsNullOrWhiteSpace(sourceTitle))
        {
            Query.Where(e => e.SourceTitle.Contains(sourceTitle));
        }

        if (!string.IsNullOrWhiteSpace(targetPostTitle))
        {
            Query.Where(e => e.TargetPostTitle.Contains(targetPostTitle));
        }

        if (startTimeUtc.HasValue)
        {
            Query.Where(e => e.PingTimeUtc >= startTimeUtc.Value);
        }

        if (endTimeUtc.HasValue)
        {
            Query.Where(e => e.PingTimeUtc <= endTimeUtc.Value);
        }
    }
}
