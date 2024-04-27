using MediatR;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Specifications;

namespace Moonglade.Comments;

public class CreateCommentCommand(Guid postId, CommentRequest payload, string ipAddress) : IRequest<CommentDetailedItem>
{
    public Guid PostId { get; set; } = postId;

    public CommentRequest Payload { get; set; } = payload;

    public string IpAddress { get; set; } = ipAddress;
}

public class CreateCommentCommandHandler(
    IBlogConfig blogConfig,
    MoongladeRepository<PostEntity> postRepo,
    MoongladeRepository<CommentEntity> commentRepo) : IRequestHandler<CreateCommentCommand, CommentDetailedItem>
{
    public async Task<CommentDetailedItem> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var spec = new PostSpec(request.PostId, false);
        var postInfo = await postRepo.FirstOrDefaultAsync(spec, p => new
        {
            p.Title,
            p.PubDateUtc
        });

        if (blogConfig.ContentSettings.CloseCommentAfterDays > 0)
        {
            var days = DateTime.UtcNow.Date.Subtract(postInfo.PubDateUtc.GetValueOrDefault()).Days;
            if (days > blogConfig.ContentSettings.CloseCommentAfterDays) return null;
        }

        var model = new CommentEntity
        {
            Id = Guid.NewGuid(),
            Username = request.Payload.Username,
            CommentContent = request.Payload.Content,
            PostId = request.PostId,
            CreateTimeUtc = DateTime.UtcNow,
            Email = request.Payload.Email,
            IPAddress = request.IpAddress,
            IsApproved = !blogConfig.ContentSettings.RequireCommentReview
        };

        await commentRepo.AddAsync(model, ct);

        var item = new CommentDetailedItem
        {
            Id = model.Id,
            CommentContent = model.CommentContent,
            CreateTimeUtc = model.CreateTimeUtc,
            Email = model.Email,
            IpAddress = model.IPAddress,
            IsApproved = model.IsApproved,
            PostTitle = postInfo.Title,
            Username = model.Username
        };

        return item;
    }
}