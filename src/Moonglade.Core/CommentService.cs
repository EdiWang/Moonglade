using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moonglade.Auditing;
using Moonglade.Configuration;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Utils;

namespace Moonglade.Core
{
    public interface ICommentService
    {
        int Count();
        Task<IReadOnlyList<Comment>> GetApprovedCommentsAsync(Guid postId);
        Task<IReadOnlyList<CommentDetailedItem>> GetCommentsAsync(int pageSize, int pageIndex);
        Task ToggleApprovalAsync(Guid[] commentIds);
        Task DeleteAsync(Guid[] commentIds);
        Task<CommentDetailedItem> CreateAsync(CommentRequest request);
        Task<CommentReply> AddReply(Guid commentId, string replyContent);
    }

    public class CommentService : ICommentService
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _audit;

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<CommentEntity> _commentRepo;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepo;
        private readonly ICommentModerator _commentModerator;

        public CommentService(
            IBlogConfig blogConfig,
            IBlogAudit audit,
            IRepository<CommentEntity> commentRepo,
            IRepository<CommentReplyEntity> commentReplyRepo,
            IRepository<PostEntity> postRepo,
            ICommentModerator commentModerator)
        {
            _blogConfig = blogConfig;
            _audit = audit;

            _commentRepo = commentRepo;
            _commentReplyRepo = commentReplyRepo;
            _postRepo = postRepo;
            _commentModerator = commentModerator;
        }

        public int Count()
        {
            return _commentRepo.Count(c => true);
        }

        public Task<IReadOnlyList<Comment>> GetApprovedCommentsAsync(Guid postId)
        {
            return _commentRepo.SelectAsync(new CommentSpec(postId), c => new Comment
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

        public Task<IReadOnlyList<CommentDetailedItem>> GetCommentsAsync(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
            }

            var spec = new CommentSpec(pageSize, pageIndex);
            var comments = _commentRepo.SelectAsync(spec, p => new CommentDetailedItem
            {
                Id = p.Id,
                CommentContent = p.CommentContent,
                CreateTimeUtc = p.CreateTimeUtc,
                Email = p.Email,
                IpAddress = p.IPAddress,
                Username = p.Username,
                IsApproved = p.IsApproved,
                PostTitle = p.Post.Title,
                CommentReplies = p.Replies.Select(cr => new CommentReplyDigest
                {
                    ReplyContent = cr.ReplyContent,
                    ReplyTimeUtc = cr.CreateTimeUtc
                }).ToList()
            });

            return comments;
        }

        public async Task ToggleApprovalAsync(Guid[] commentIds)
        {
            if (commentIds is null || !commentIds.Any())
            {
                throw new ArgumentNullException(nameof(commentIds));
            }

            var spec = new CommentSpec(commentIds);
            var comments = await _commentRepo.GetAsync(spec);
            foreach (var cmt in comments)
            {
                cmt.IsApproved = !cmt.IsApproved;
                await _commentRepo.UpdateAsync(cmt);

                string logMessage = $"Updated comment approval status to '{cmt.IsApproved}' for comment id: '{cmt.Id}'";
                await _audit.AddAuditEntry(
                    EventType.Content, cmt.IsApproved ? AuditEventId.CommentApproval : AuditEventId.CommentDisapproval, logMessage);
            }
        }

        public async Task DeleteAsync(Guid[] commentIds)
        {
            if (commentIds is null || !commentIds.Any())
            {
                throw new ArgumentNullException(nameof(commentIds));
            }

            var spec = new CommentSpec(commentIds);
            var comments = await _commentRepo.GetAsync(spec);
            foreach (var cmt in comments)
            {
                // 1. Delete all replies
                var cReplies = await _commentReplyRepo.GetAsync(new CommentReplySpec(cmt.Id));
                if (cReplies.Any())
                {
                    _commentReplyRepo.Delete(cReplies);
                }

                // 2. Delete comment itself
                _commentRepo.Delete(cmt);
                await _audit.AddAuditEntry(EventType.Content, AuditEventId.CommentDeleted, $"Comment '{cmt.Id}' deleted.");
            }
        }

        public async Task<CommentDetailedItem> CreateAsync(CommentRequest request)
        {
            if (_blogConfig.ContentSettings.EnableWordFilter)
            {
                switch (_blogConfig.ContentSettings.WordFilterMode)
                {
                    case WordFilterMode.Mask:
                        request.Username = await _commentModerator.ModerateContent(request.Username);
                        request.Content = await _commentModerator.ModerateContent(request.Content);
                        break;
                    case WordFilterMode.Block:
                        if (await _commentModerator.HasBadWord(request.Username, request.Content))
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
                Username = request.Username,
                CommentContent = request.Content,
                PostId = request.PostId,
                CreateTimeUtc = DateTime.UtcNow,
                Email = request.Email,
                IPAddress = request.IpAddress,
                IsApproved = !_blogConfig.ContentSettings.RequireCommentReview
            };

            await _commentRepo.AddAsync(model);

            var spec = new PostSpec(request.PostId, false);
            var postTitle = _postRepo.SelectFirstOrDefault(spec, p => p.Title);

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

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CommentReplied, $"Replied comment id '{commentId}'");
            return reply;
        }
    }
}
