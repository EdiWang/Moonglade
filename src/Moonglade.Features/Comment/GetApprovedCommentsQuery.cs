using LiteBus.Queries.Abstractions;
using Moonglade.Data.DTO;

namespace Moonglade.Features.Comment;

public record GetApprovedCommentsQuery(Guid PostId) : IQuery<List<Data.DTO.Comment>>;

public class GetApprovedCommentsQueryHandler(BlogDbContext db) : IQueryHandler<GetApprovedCommentsQuery, List<Data.DTO.Comment>>
{
    public Task<List<Data.DTO.Comment>> HandleAsync(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        return db.Comment.AsNoTracking()
            .Include(c => c.Replies)
            .Where(c => c.PostId == request.PostId && c.IsApproved)
            .Select(c => new Data.DTO.Comment
            {
                Username = c.Username,
                Email = c.Email,
                CreateTimeUtc = c.CreateTimeUtc,
                CommentContent = c.CommentContent,
                Replies = c.Replies.Select(cr => new CommentReplyDigest
                {
                    ReplyContent = cr.ReplyContent,
                    ReplyTimeUtc = cr.CreateTimeUtc
                }).ToList()
            })
            .ToListAsync(ct);
    }
}