using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public record GetApprovedCommentsQuery(Guid PostId) : IRequest<List<Comment>>;

public class GetApprovedCommentsQueryHandler(MoongladeRepository<CommentEntity> repo) : IRequestHandler<GetApprovedCommentsQuery, List<Comment>>
{
    public Task<List<Comment>> Handle(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(new CommentWithRepliesSpec(request.PostId), c => new Comment
        {
            CommentContent = c.CommentContent,
            CreateTimeUtc = c.CreateTimeUtc,
            Username = c.Username,
            Email = c.Email,
            CommentReplies = c.Replies.Select(cr => new CommentReplyDigest
            {
                ReplyContent = cr.ReplyContent,
                ReplyTimeUtc = cr.CreateTimeUtc
            }).ToList()
        }, ct);
    }
}