using MediatR;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Comments
{
    public class CreateCommentCommand : IRequest<CommentDetailedItem>
    {
        public CreateCommentCommand(Guid postId, CommentRequest request, string ipAddress)
        {
            PostId = postId;
            Request = request;
            IpAddress = ipAddress;
        }

        public Guid PostId { get; set; }

        public CommentRequest Request { get; set; }

        public string IpAddress { get; set; }
    }

    public class CreateCommentCommandHandler : IRequestHandler<CreateCommentCommand, CommentDetailedItem>
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IRepository<PostEntity> _postRepo;
        private readonly ICommentModerator _commentModerator;
        private readonly IRepository<CommentEntity> _commentRepo;

        public CreateCommentCommandHandler(
            IBlogConfig blogConfig, IRepository<PostEntity> postRepo, ICommentModerator commentModerator, IRepository<CommentEntity> commentRepo)
        {
            _blogConfig = blogConfig;
            _postRepo = postRepo;
            _commentModerator = commentModerator;
            _commentRepo = commentRepo;
        }

        public async Task<CommentDetailedItem> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            if (_blogConfig.ContentSettings.EnableWordFilter)
            {
                switch (_blogConfig.ContentSettings.WordFilterMode)
                {
                    case WordFilterMode.Mask:
                        request.Request.Username = await _commentModerator.ModerateContent(request.Request.Username);
                        request.Request.Content = await _commentModerator.ModerateContent(request.Request.Content);
                        break;
                    case WordFilterMode.Block:
                        if (await _commentModerator.HasBadWord(request.Request.Username, request.Request.Content))
                        {
                            await Task.CompletedTask;
                            return null;
                        }
                        break;
                }
            }

            var model = new CommentEntity
            {
                Id = Guid.NewGuid(),
                Username = request.Request.Username,
                CommentContent = request.Request.Content,
                PostId = request.PostId,
                CreateTimeUtc = DateTime.UtcNow,
                Email = request.Request.Email,
                IPAddress = request.IpAddress,
                IsApproved = !_blogConfig.ContentSettings.RequireCommentReview
            };

            await _commentRepo.AddAsync(model);

            var spec = new PostSpec(request.PostId, false);
            var postTitle = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Title);

            var item = new CommentDetailedItem
            {
                Id = model.Id,
                CommentContent = model.CommentContent,
                CreateTimeUtc = model.CreateTimeUtc,
                Email = model.Email,
                IpAddress = model.IPAddress,
                IsApproved = model.IsApproved,
                PostTitle = postTitle,
                Username = model.Username
            };

            return item;
        }
    }
}
