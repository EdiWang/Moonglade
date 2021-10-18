using MediatR;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public class GetApprovedCommentsQuery : IRequest<IReadOnlyList<Comment>>
{
    public GetApprovedCommentsQuery(Guid postId)
    {
        PostId = postId;
    }

    public Guid PostId { get; set; }
}

public class GetApprovedCommentsQueryHandler : IRequestHandler<GetApprovedCommentsQuery, IReadOnlyList<Comment>>
{
    private readonly IRepository<CommentEntity> _commentRepo;

    public GetApprovedCommentsQueryHandler(IRepository<CommentEntity> commentRepo)
    {
        _commentRepo = commentRepo;
    }

    public Task<IReadOnlyList<Comment>> Handle(GetApprovedCommentsQuery request, CancellationToken cancellationToken)
    {
        return _commentRepo.SelectAsync(new CommentSpec(request.PostId), c => new Comment
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
        });
    }
}