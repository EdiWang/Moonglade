using Moonglade.Data.DTO;

namespace Moonglade.Features.Post;

public static class PostQueryExtensions
{
    public static IQueryable<PostEntity> FilterByStatus(this IQueryable<PostEntity> query, PostStatus status) =>
        status switch
        {
            PostStatus.Draft => query.Where(p => p.PostStatus == PostStatus.Draft && !p.IsDeleted),
            PostStatus.Scheduled => query.Where(p => p.PostStatus == PostStatus.Scheduled && !p.IsDeleted),
            PostStatus.Published => query.Where(p => p.PostStatus == PostStatus.Published && !p.IsDeleted),
            PostStatus.Deleted => query.Where(p => p.IsDeleted),
            PostStatus.Default => query,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    public static IQueryable<PostSegment> SelectToSegment(this IQueryable<PostEntity> query) =>
        query.Select(p => new PostSegment
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PubDateUtc = p.PubDateUtc,
            PostStatus = p.PostStatus,
            IsFeatured = p.IsFeatured,
            IsDeleted = p.IsDeleted,
            IsOutdated = p.IsOutdated,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ScheduledPublishTimeUtc = p.ScheduledPublishTimeUtc,
            ContentAbstract = p.ContentAbstract
        });
}
