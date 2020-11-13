using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edi.WordFilter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Configuration.Abstraction;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class CommentService : BlogService
    {
        private readonly IBlogConfig _blogConfig;
        private readonly IBlogAudit _audit;

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<CommentEntity> _commentRepo;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepo;

        public CommentService(
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IBlogAudit audit,
            IRepository<CommentEntity> commentRepo,
            IRepository<CommentReplyEntity> commentReplyRepo,
            IRepository<PostEntity> postRepo) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _audit = audit;

            _commentRepo = commentRepo;
            _commentReplyRepo = commentReplyRepo;
            _postRepo = postRepo;
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
                CreateOnUtc = c.CreateOnUtc,
                Username = c.Username,
                Email = c.Email,
                CommentReplies = c.CommentReply.Select(cr => new CommentReplyDigest
                {
                    ReplyContent = cr.ReplyContent,
                    ReplyTimeUtc = cr.ReplyTimeUtc
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
                CreateOnUtc = p.CreateOnUtc,
                Email = p.Email,
                IpAddress = p.IPAddress,
                Username = p.Username,
                IsApproved = p.IsApproved,
                PostTitle = p.Post.Title,
                CommentReplies = p.CommentReply.Select(cr => new CommentReplyDigest
                {
                    ReplyContent = cr.ReplyContent,
                    ReplyTimeUtc = cr.ReplyTimeUtc
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
                Logger.LogInformation(logMessage);
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
                var dw = _blogConfig.ContentSettings.DisharmonyWords;
                var maskWordFilter = new MaskWordFilter(new StringWordSource(dw));
                request.Username = maskWordFilter.FilterContent(request.Username);
                request.Content = maskWordFilter.FilterContent(request.Content);
            }

            var model = new CommentEntity
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                CommentContent = request.Content,
                PostId = request.PostId,
                CreateOnUtc = DateTime.UtcNow,
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
                CreateOnUtc = model.CreateOnUtc,
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
                ReplyTimeUtc = DateTime.UtcNow,
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
                ReplyTimeUtc = model.ReplyTimeUtc,
                Slug = cmt.Post.Slug,
                Title = cmt.Post.Title
            };

            await _audit.AddAuditEntry(EventType.Content, AuditEventId.CommentReplied, $"Replied comment id '{commentId}'");
            return reply;
        }
    }
}
