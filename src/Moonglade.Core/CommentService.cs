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
        private readonly BlogConfig _blogConfig;

        private readonly IRepository<Comment> _commentRepository;

        private readonly IRepository<CommentReply> _commentReplyRepository;

        public CommentService(
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            IRepository<Comment> commentRepository,
            IRepository<CommentReply> commentReplyRepository) : base(logger, settings)
        {
            _blogConfig = blogConfig;
            _commentRepository = commentRepository;
            _commentReplyRepository = commentReplyRepository;
            _blogConfig.GetConfiguration(blogConfigurationService);
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
                PubDateUtc = p.CreateOnUtc
            });
        }

        public IReadOnlyList<Comment> GetPagedComment(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
            }

            var spec = new PagedCommentSepc(pageSize, pageIndex);
            var comments = _commentRepository.Get(spec, false);
            return comments;
        }

        public Response SetApprovalStatus(Guid commentId, bool isApproved)
        {
            try
            {
                var comment = _commentRepository.Get(commentId);
                if (null != comment)
                {
                    int rows;
                    if (isApproved)
                    {
                        Logger.LogInformation($"Approve comment {commentId}");
                        comment.IsApproved = true;
                        rows = _commentRepository.Update(comment);
                    }
                    else
                    {
                        Logger.LogInformation($"Disapprove and delete comment {commentId}");
                        rows = _commentRepository.Delete(comment);
                    }

                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.CommentNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(SetApprovalStatus)}");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response Delete(Guid commentId)
        {
            try
            {
                var comment = _commentRepository.Get(commentId);
                if (null != comment)
                {
                    // 1. Delete all replies
                    var cReplies = _commentReplyRepository.Get(new CommentReplySpec(commentId));
                    if (cReplies.Any())
                    {
                        _commentReplyRepository.Delete(cReplies);
                    }

                    // 2. Delete comment itself
                    var rows = _commentRepository.Delete(comment);
                    return new Response(rows > 0);
                }
                return new FailedResponse((int)ResponseFailureCode.CommentNotFound);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(Delete)}(commentId: {commentId})");
                return new FailedResponse((int)ResponseFailureCode.GeneralException);
            }
        }

        public Response<Comment> NewComment(string username, string commentContent, Guid postId,
            string email, string ipAddress, string userAgent)
        {
            try
            {
                // 1. Check comment enabled or not
                if (!_blogConfig.EnableComments)
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
                    var dw = _blogConfig.DisharmonyWords;
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
                if (!_blogConfig.EnableComments)
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
                    PubDateUTC = cmt.Post.PostPublish.PubDateUtc,
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
