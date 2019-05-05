using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using Edi.Practice.RequestResponseModel;
using Edi.WordFilter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class CommentService : MoongladeService
    {
        private readonly IBlogConfig _blogConfig;

        private readonly IRepository<Comment> _commentRepository;

        private readonly IRepository<CommentReply> _commentReplyRepository;

        public CommentService(
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            IBlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            IRepository<Comment> commentRepository,
            IRepository<CommentReply> commentReplyRepository) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _commentRepository = commentRepository;
            _commentReplyRepository = commentReplyRepository;
            _blogConfig.Initialize(blogConfigurationService);
        }

        public int CountForApproved => _commentRepository.Count(c => c.IsApproved);

        public async Task<Response<IReadOnlyList<Comment>>> GetRecentCommentsAsync(int top)
        {
            try
            {
                var spec = new RecentCommentSpec(top);
                var list = await _commentRepository.GetAsync(spec);

                return new SuccessResponse<IReadOnlyList<Comment>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetRecentCommentsAsync)}");
                return new FailedResponse<IReadOnlyList<Comment>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public IReadOnlyList<Comment> GetApprovedCommentsOfPost(Guid postId)
        {
            return _commentRepository.Get(new CommentOfPostSpec(postId), false);
        }

        public IReadOnlyList<CommentGridModel> GetPendingApprovalComments()
        {
            return _commentRepository.Select(new PendingApprovalCommentSepc(), p => new CommentGridModel
            {
                Id = p.Id,
                Username = p.Username,
                Email = p.Email,
                IpAddress = p.IPAddress,
                CommentContent = p.CommentContent,
                PostTitle = p.Post.Title,
                CreateOnUtc = p.CreateOnUtc
            });
        }

        public async Task<IReadOnlyList<Comment>> GetPagedCommentAsync(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
            }

            var spec = new PagedCommentSepc(pageSize, pageIndex);
            var comments = await _commentRepository.GetAsync(spec, false);
            return comments;
        }

        public async Task<Response> ApproveComments(Guid[] commentIds)
        {
            try
            {
                if (null == commentIds || !commentIds.Any())
                {
                    throw new ArgumentNullException(nameof(commentIds));
                }

                var spec = new CommentInIdSpec(commentIds);
                var comments = await _commentRepository.GetAsync(spec);
                foreach (var cmt in comments)
                {
                    cmt.IsApproved = true;
                    await _commentRepository.UpdateAsync(cmt);
                }
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(ApproveComments)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public async Task<Response> DeleteComments(Guid[] commentIds)
        {
            try
            {
                var spec = new CommentInIdSpec(commentIds);
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
                }
                return new SuccessResponse();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(DeleteComments)}()");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<Comment> NewComment(string username, string commentContent, Guid postId,
            string email, string ipAddress, string userAgent)
        {
            try
            {
                // 1. Check comment enabled or not
                if (!_blogConfig.ContentSettings.EnableComments)
                {
                    return new FailedResponse<Comment>((int)ResponseFailureCode.CommentDisabled);
                }

                // 2. Check user email domain
                var bannedDomains = _blogConfig.EmailConfiguration.BannedMailDomain;
                if (bannedDomains.Any())
                {
                    var address = new MailAddress(email);
                    if (bannedDomains.Contains(address.Host))
                    {
                        return new FailedResponse<Comment>((int)ResponseFailureCode.EmailDomainBlocked);
                    }
                }

                // 3. Encode HTML
                username = HttpUtility.HtmlEncode(username);

                // 4. Harmonize banned keywords
                if (AppSettings.EnableHarmonizor)
                {
                    var dw = _blogConfig.ContentSettings.DisharmonyWords;
                    var maskWordFilter = new MaskWordFilter(new StringWordSource(dw));
                    username = maskWordFilter.FilterContent(username);
                    commentContent = maskWordFilter.FilterContent(commentContent);
                }

                var model = new Comment
                {
                    Id = Guid.NewGuid(),
                    Username = username,
                    CommentContent = commentContent,
                    PostId = postId,
                    CreateOnUtc = DateTime.UtcNow,
                    Email = email,
                    IPAddress = ipAddress,
                    IsApproved = false,
                    UserAgent = userAgent
                };

                _commentRepository.Add(model);
                return new SuccessResponse<Comment>(model);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(NewComment)}");
                return new FailedResponse<Comment>((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<CommentReplySummary> NewReply(Guid commentId, string replyContent, string ipAddress, string userAgent)
        {
            try
            {
                if (!_blogConfig.ContentSettings.EnableComments)
                {
                    return new FailedResponse<CommentReplySummary>((int)ResponseFailureCode.CommentDisabled);
                }

                var cmt = _commentRepository.Get(commentId);

                if (null == cmt)
                {
                    return new FailedResponse<CommentReplySummary>((int)ResponseFailureCode.CommentNotFound);
                }

                var id = Guid.NewGuid();
                var model = new CommentReply
                {
                    Id = id,
                    ReplyContent = replyContent,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    ReplyTimeUtc = DateTime.UtcNow,
                    CommentId = commentId
                };

                _commentReplyRepository.Add(model);

                var summary = new CommentReplySummary
                {
                    CommentContent = cmt.CommentContent,
                    CommentId = commentId,
                    Email = cmt.Email,
                    Id = model.Id,
                    IpAddress = model.IpAddress,
                    PostId = cmt.PostId,
                    PubDateUtc = cmt.Post.PostPublish.PubDateUtc,
                    ReplyContent = model.ReplyContent,
                    ReplyTimeUtc = model.ReplyTimeUtc,
                    Slug = cmt.Post.Slug,
                    Title = cmt.Post.Title,
                    UserAgent = model.UserAgent
                };

                return new SuccessResponse<CommentReplySummary>(summary);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(NewReply)}");
                return new FailedResponse<CommentReplySummary>((int)ResponseFailureCode.GeneralException);
            }
        }
    }
}
