using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Threading.Tasks;

namespace Moonglade.Comments
{
    public interface ICommentService
    {
        Task<CommentReply> AddReply(Guid commentId, string replyContent);
    }

    public class CommentService : ICommentService
    {
        private readonly IBlogAudit _audit;

        private readonly IRepository<CommentEntity> _commentRepo;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

        public CommentService(
            IBlogAudit audit,
            IRepository<CommentEntity> commentRepo,
            IRepository<CommentReplyEntity> commentReplyRepo)
        {
            _audit = audit;

            _commentRepo = commentRepo;
            _commentReplyRepo = commentReplyRepo;
        }

        public async Task<CommentReply> AddReply(Guid commentId, string replyContent)
        {
            var cmt = await _commentRepo.GetAsync(commentId);
            if (cmt is null) throw new InvalidOperationException($"Comment {commentId} is not found.");

            var id = Guid.NewGuid();
            var model = new CommentReplyEntity
            {
                Id = id,
                ReplyContent = replyContent,
                CreateTimeUtc = DateTime.UtcNow,
                CommentId = commentId
            };

            await _commentReplyRepo.AddAsync(model);

            var reply = new CommentReply
            {
                CommentContent = cmt.CommentContent,
                CommentId = commentId,
                Email = cmt.Email,
                Id = model.Id,
                PostId = cmt.PostId,
                PubDateUtc = cmt.Post.PubDateUtc.GetValueOrDefault(),
                ReplyContent = model.ReplyContent,
                ReplyContentHtml = ContentProcessor.MarkdownToContent(model.ReplyContent, ContentProcessor.MarkdownConvertType.Html),
                ReplyTimeUtc = model.CreateTimeUtc,
                Slug = cmt.Post.Slug,
                Title = cmt.Post.Title
            };

            await _audit.AddEntry(BlogEventType.Content, BlogEventId.CommentReplied, $"Replied comment id '{commentId}'");
            return reply;
        }
    }
}
