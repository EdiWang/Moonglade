using MediatR;
using Microsoft.Extensions.Logging;
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
    ILogger<CreateCommentCommandHandler> logger,
    MoongladeRepository<PostEntity> postRepo,
    MoongladeRepository<CommentEntity> commentRepo) : IRequestHandler<CreateCommentCommand, CommentDetailedItem>
{
    public async Task<CommentDetailedItem> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var spec = new PostByIdForTitleDateSpec(request.PostId);
        var postInfo = await postRepo.FirstOrDefaultAsync(spec, ct);

        if (blogConfig.CommentSettings.CloseCommentAfterDays > 0)
        {
            var days = DateTime.UtcNow.Date.Subtract(postInfo.PubDateUtc.GetValueOrDefault()).Days;
            if (days > blogConfig.CommentSettings.CloseCommentAfterDays) return null;
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
            IsApproved = !blogConfig.CommentSettings.RequireCommentReview
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

        logger.LogInformation("New comment created: {0}", item.Id);
        return item;
    }
}