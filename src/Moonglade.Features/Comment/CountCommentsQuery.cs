using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Comment;

public record CountCommentsQuery(CommentFilter Filter) : IQuery<int>;

public class CountCommentsQueryHandler(BlogDbContext db) : IQueryHandler<CountCommentsQuery, int>
{
    public Task<int> HandleAsync(CountCommentsQuery request, CancellationToken ct)
    {
        var filter = request.Filter;

        IQueryable<CommentEntity> query = db.Comment.AsNoTracking();

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

        return query.CountAsync(ct);
    }
}