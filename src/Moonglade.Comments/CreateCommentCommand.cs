using MediatR;
using Moonglade.Comments.Moderator;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Comments;

public class CreateCommentCommand(Guid postId, CommentRequest payload, string ipAddress) : IRequest<(int Status, CommentDetailedItem Item)>
{
    public Guid PostId { get; set; } = postId;

    public CommentRequest Payload { get; set; } = payload;

    public string IpAddress { get; set; } = ipAddress;
}

public class CreateCommentCommandHandler(IBlogConfig blogConfig, IRepository<PostEntity> postRepo, IModeratorService moderator, IRepository<CommentEntity> commentRepo) :
        IRequestHandler<CreateCommentCommand, (int Status, CommentDetailedItem Item)>
{
    public async Task<(int Status, CommentDetailedItem Item)> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        if (blogConfig.ContentSettings.EnableWordFilter)
        {
            switch (blogConfig.ContentSettings.WordFilterMode)
            {
                case WordFilterMode.Mask:
                    request.Payload.Username = await moderator.Mask(request.Payload.Username);
                    request.Payload.Content = await moderator.Mask(request.Payload.Content);
                    break;
                case WordFilterMode.Block:
                    if (await moderator.Detect(request.Payload.Username, request.Payload.Content))
                    {
                        await Task.CompletedTask;
                        return (-1, null);
                    }
                    break;
            }
        }

        var spec = new PostSpec(request.PostId, false);
        var postInfo = await postRepo.FirstOrDefaultAsync(spec, p => new
        {
            p.Title,
            p.PubDateUtc
        });

        if (blogConfig.ContentSettings.CloseCommentAfterDays > 0)
        {
            var days = DateTime.UtcNow.Date.Subtract(postInfo.PubDateUtc.GetValueOrDefault()).Days;
            if (days > blogConfig.ContentSettings.CloseCommentAfterDays)
            {
                return (-2, null);
            }
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

        return (0, item);
    }
}