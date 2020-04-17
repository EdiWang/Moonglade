using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
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
using EventId = Moonglade.Auditing.EventId;

namespace Moonglade.Core
{
    public class CommentService : MoongladeService
    {
        private readonly IBlogConfig _blogConfig;

        private readonly IRepository<PostEntity> _postRepository;
        private readonly IRepository<CommentEntity> _commentRepository;
        private readonly IRepository<CommentReplyEntity> _commentReplyRepository;
        private readonly IMoongladeAudit _moongladeAudit;

        public CommentService(
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            IRepository<CommentEntity> commentRepository,
            IRepository<CommentReplyEntity> commentReplyRepository,
            IRepository<PostEntity> postRepository,
            IMoongladeAudit moongladeAudit) : base(logger, settings)
        {
            _blogConfig = blogConfig;

            _commentRepository = commentRepository;
            _commentReplyRepository = commentReplyRepository;
            _postRepository = postRepository;
            _moongladeAudit = moongladeAudit;
        }

        public int CountComments()
        {
            return _commentRepository.Count(c => true);
        }

        public Task<IReadOnlyList<PostCommentListItem>> GetSelectedCommentsOfPostAsync(Guid postId)
        {
            return _commentRepository.SelectAsync(new CommentSpec(postId), c => new PostCommentListItem
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

        public Task<Response<IReadOnlyList<CommentListItem>>> GetPagedCommentAsync(int pageSize, int pageIndex)
        {
            return TryExecuteAsync<IReadOnlyList<CommentListItem>>(async () =>
            {
                if (pageSize < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
                }

                var spec = new CommentSpec(pageSize, pageIndex);
                var comments = await _commentRepository.SelectAsync(spec, p => new CommentListItem
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

                return new SuccessResponse<IReadOnlyList<CommentListItem>>(comments);
            });
        }

        public Task<Response> ToggleApprovalStatusAsync(Guid[] commentIds)
        {
            return TryExecuteAsync(async () =>
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
                    await _moongladeAudit.AddAuditEntry(
                        EventType.Content, cmt.IsApproved ? EventId.CommentApproval : EventId.CommentDisapproval, logMessage);
                }

                return new SuccessResponse();
            });
        }

        public Task<Response> DeleteCommentsAsync(Guid[] commentIds)
        {
            return TryExecuteAsync(async () =>
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
                    await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CommentDeleted, $"Comment '{cmt.Id}' deleted.");
                }

                return new SuccessResponse();
            });
        }

        public Task<Response<CommentListItem>> AddCommentAsync(NewCommentRequest request)
        {
            return TryExecuteAsync<CommentListItem>(async () =>
            {
                // 1. Check comment enabled or not
                if (!_blogConfig.ContentSettings.EnableComments)
                {
                    return new FailedResponse<CommentListItem>((int)ResponseFailureCode.CommentDisabled);
                }

                // 2. Check user email domain
                var bannedDomains = _blogConfig.EmailSettings.BannedMailDomain?.Split(",");
                if (null != bannedDomains && bannedDomains.Any())
                {
                    var address = new MailAddress(request.Email);
                    if (bannedDomains.Contains(address.Host))
                    {
                        Logger.LogWarning($"Email host '{address.Host}' is found in ban list, rejecting comments.");
                        return new FailedResponse<CommentListItem>((int)ResponseFailureCode.EmailDomainBlocked);
                    }
                }

                // 3. Harmonize banned keywords
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
                    IsApproved = !_blogConfig.ContentSettings.RequireCommentReview,
                    UserAgent = request.UserAgent
                };

                await _commentRepository.AddAsync(model);

                var spec = new PostSpec(request.PostId, false);
                var postTitle = _postRepository.SelectFirstOrDefault(spec, p => p.Title);

                var item = new CommentListItem
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

                return new SuccessResponse<CommentListItem>(item);
            });
        }

        public Task<Response<CommentReplyDetail>> AddReply(Guid commentId, string replyContent, string ipAddress, string userAgent)
        {
            return TryExecuteAsync<CommentReplyDetail>(async () =>
            {
                if (!_blogConfig.ContentSettings.EnableComments)
                {
                    return new FailedResponse<CommentReplyDetail>((int)ResponseFailureCode.CommentDisabled);
                }

                var cmt = _commentRepository.Get(commentId);

                if (null == cmt)
                {
                    return new FailedResponse<CommentReplyDetail>((int)ResponseFailureCode.CommentNotFound);
                }

                var id = Guid.NewGuid();
                var model = new CommentReplyEntity
                {
                    Id = id,
                    ReplyContent = replyContent,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ReplyTimeUtc = DateTime.UtcNow,
                    CommentId = commentId
                };

                _commentReplyRepository.Add(model);

                var detail = new CommentReplyDetail
                {
                    CommentContent = cmt.CommentContent,
                    CommentId = commentId,
                    Email = cmt.Email,
                    Id = model.Id,
                    IpAddress = model.IpAddress,
                    PostId = cmt.PostId,
                    PubDateUtc = cmt.Post.PostPublish.PubDateUtc.GetValueOrDefault(),
                    ReplyContent = model.ReplyContent,
                    ReplyContentHtml = Utils.ConvertMarkdownContent(model.ReplyContent, Utils.MarkdownConvertType.Html),
                    ReplyTimeUtc = model.ReplyTimeUtc,
                    Slug = cmt.Post.Slug,
                    Title = cmt.Post.Title,
                    UserAgent = model.UserAgent
                };

                await _moongladeAudit.AddAuditEntry(EventType.Content, EventId.CommentReplied, $"Replied comment id '{commentId}'");
                return new SuccessResponse<CommentReplyDetail>(detail);
            });
        }
    }
}
