using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public record GetApprovedCommentsQuery(Guid PostId) : IRequest<IReadOnlyList<Comment>>;

public class GetApprovedCommentsQueryHandler(IRepository<CommentEntity> repo) : IRequestHandler<GetApprovedCommentsQuery, IReadOnlyList<Comment>>
{
    public Task<IReadOnlyList<Comment>> Handle(GetApprovedCommentsQuery request, CancellationToken ct)
    {
        return repo.SelectAsync(new CommentSpec(request.PostId), c => new Comment
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