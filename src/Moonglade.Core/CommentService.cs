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

        private readonly IRepository<PostEntity> _postRepository;
        private readonly IRepository<CommentEntity> _commentRepository;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepository;
        private readonly IBlogAudit _blogAudit;

        public CommentService(
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IRepository<CommentEntity> commentRepository,
            IRepository<CommentReplyEntity> commentReplyRepository,
            IRepository<PostEntity> postRepository,
            IBlogAudit blogAudit) : base(logger, settings)
        {
            _blogConfig = blogConfig;

            _commentRepository = commentRepository;
            _commentReplyRepository = commentReplyRepository;
            _postRepository = postRepository;
            _blogAudit = blogAudit;
        }

        public int CountComments()
        {
            return _commentRepository.Count(c => true);
        }

        public Task<IReadOnlyList<CommentItem>> GetSelectedCommentsOfPostAsync(Guid postId)
        {
            return _commentRepository.SelectAsync(new CommentSpec(postId), c => new CommentItem
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

        public Task<IReadOnlyList<CommentDetailedItem>> GetPagedCommentAsync(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
            }

            var spec = new CommentSpec(pageSize, pageIndex);
            var comments = _commentRepository.SelectAsync(spec, p => new CommentDetailedItem
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

        public async Task ToggleApprovalStatusAsync(Guid[] commentIds)
        {
            if (null == commentIds || !commentIds.Any())
            {
                throw new ArgumentNullException(nameof(commentIds));
            }

            var spec = new CommentSpec(commentIds);
            var comments = await _commentRepository.GetAsync(spec);
            foreach (var cmt in comments)
            {
                cmt.IsApproved = !cmt.IsApproved;
                await _commentRepository.UpdateAsync(cmt);

                string logMessage = $"Updated comment approval status to '{cmt.IsApproved}' for comment id: '{cmt.Id}'";
                Logger.LogInformation(logMessage);
                await _blogAudit.AddAuditEntry(
                    EventType.Content, cmt.IsApproved ? AuditEventId.CommentApproval : AuditEventId.CommentDisapproval, logMessage);
            }
        }

        public async Task DeleteAsync(Guid[] commentIds)
        {
            if (null == commentIds || !commentIds.Any())
            {
                throw new ArgumentNullException(nameof(commentIds));
            }

            var spec = new CommentSpec(commentIds);
            var comments = await _commentRepository.GetAsync(spec);
            foreach (var cmt in comments)
            {
                // 1. Delete all replies
                var cReplies = await _commentReplyRepository.GetAsync(new CommentReplySpec(cmt.Id));
                if (cReplies.Any())
                {
                    _commentReplyRepository.Delete(cReplies);
                }

                // 2. Delete comment itself
                _commentRepository.Delete(cmt);
                await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CommentDeleted, $"Comment '{cmt.Id}' deleted.");
            }
        }

        public async Task<CommentDetailedItem> CreateAsync(NewCommentRequest request)
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

            await _commentRepository.AddAsync(model);

            var spec = new PostSpec(request.PostId, false);
            var postTitle = _postRepository.SelectFirstOrDefault(spec, p => p.Title);

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

        public async Task<CommentReplyDetail> AddReply(Guid commentId, string replyContent)
        {
            var cmt = await _commentRepository.GetAsync(commentId);

            if (null == cmt)
            {
                throw new InvalidOperationException($"Comment {commentId} is not found.");
            }

            var id = Guid.NewGuid();
            var model = new CommentReplyEntity
            {
                Id = id,
                ReplyContent = replyContent,
                ReplyTimeUtc = DateTime.UtcNow,
                CommentId = commentId
            };

            await _commentReplyRepository.AddAsync(model);

            var detail = new CommentReplyDetail
            {
                CommentContent = cmt.CommentContent,
                CommentId = commentId,
                Email = cmt.Email,
                Id = model.Id,
                PostId = cmt.PostId,
                PubDateUtc = cmt.Post.PubDateUtc.GetValueOrDefault(),
                ReplyContent = model.ReplyContent,
                ReplyContentHtml = Utils.MarkdownToContent(model.ReplyContent, Utils.MarkdownConvertType.Html),
                ReplyTimeUtc = model.ReplyTimeUtc,
                Slug = cmt.Post.Slug,
                Title = cmt.Post.Title
            };

            await _blogAudit.AddAuditEntry(EventType.Content, AuditEventId.CommentReplied, $"Replied comment id '{commentId}'");
            return detail;
        }
    }
}
