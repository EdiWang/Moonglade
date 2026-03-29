using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;
using Moonglade.Data.Specifications;

namespace Moonglade.Features.Comment;

public record ListCommentsQuery(int PageSize, int PageIndex, CommentFilter Filter) : IQuery<List<CommentDetailedItem>>;

public class ListCommentsQueryHandler(BlogDbContext db) : IQueryHandler<ListCommentsQuery, List<CommentDetailedItem>>
{
    public Task<List<CommentDetailedItem>> HandleAsync(ListCommentsQuery request, CancellationToken ct)
    {
        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                $"{nameof(request.PageSize)} can not be less than 1, current value: {request.PageSize}.");
        }

        var startRow = (request.PageIndex - 1) * request.PageSize;
        var filter = request.Filter;

        IQueryable<CommentEntity> query = db.Comment.AsNoTracking()
            .Include(c => c.Post)
            .Include(c => c.Replies);

        if (!string.IsNullOrWhiteSpace(filter.Username))
        {
            query = query.Where(c => c.Username.Contains(filter.Username));
        }

        if (!string.IsNullOrWhiteSpace(filter.Email))
        {
            query = query.Where(c => c.Email.Contains(filter.Email));
        }

        if (!string.IsNullOrWhiteSpace(filter.CommentContent))
        {
            query = query.Where(c => c.CommentContent.Contains(filter.CommentContent));
        }

        if (filter.StartTimeUtc.HasValue)
        {
            query = query.Where(c => c.CreateTimeUtc >= filter.StartTimeUtc.Value);
        }

        if (filter.EndTimeUtc.HasValue)
        {
            query = query.Where(c => c.CreateTimeUtc <= filter.EndTimeUtc.Value);
        }

        return query
            .OrderByDescending(c => c.CreateTimeUtc)
            .Skip(startRow)
            .Take(request.PageSize)
            .Select(c => new CommentDetailedItem
            {
                Id = c.Id,
                CommentContent = c.CommentContent,
                CreateTimeUtc = c.CreateTimeUtc,
                Email = c.Email,
                IpAddress = c.IPAddress,
                Username = c.Username,
                IsApproved = c.IsApproved,
                PostTitle = c.Post.Title,
                Replies = c.Replies.Select(cr => new CommentReplyDigest
                {
                    ReplyContent = cr.ReplyContent,
                    ReplyTimeUtc = cr.CreateTimeUtc
                }).ToList()
            })
            .ToListAsync(ct);
    }
}