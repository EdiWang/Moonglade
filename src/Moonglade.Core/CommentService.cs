using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using Edi.Practice.RequestResponseModel;
using Edi.WordFilter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class CommentService : MoongladeService
    {
        private readonly BlogConfig _blogConfig;

        public CommentService(MoongladeDbContext context,
            ILogger<CommentService> logger,
            IOptions<AppSettings> settings,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService) : base(context, logger, settings)
        {
            _blogConfig = blogConfig;
            _blogConfig.GetConfiguration(blogConfigurationService);
        }

        public int CountForPublic => Context.Comment.Count(c => c.IsApproved.GetValueOrDefault());

        public async Task<Response<List<Comment>>> GetRecentCommentsAsync(int top)
        {
            try
            {
                var recentComments = Context.Comment.Include(c => c.Post)
                                            .ThenInclude(p => p.PostPublish)
                                            .Where(c => c.IsApproved.Value)
                                            .OrderByDescending(c => c.CreateOnUtc)
                                            .Take(top)
                                            .AsNoTracking();

                var list = await recentComments.ToListAsync();
                return new SuccessResponse<List<Comment>>(list);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(GetRecentCommentsAsync)}");
                return new FailedResponse<List<Comment>>((int)ResponseFailureCode.GeneralException);
            }
        }

        public List<Comment> GetApprovedCommentsOfPost(Guid postId)
        {
            var comments = Context.Comment.Include(c => c.CommentReply)
                                          .Where(c => c.PostId == postId &&
                                                      c.IsApproved != null &&
                                                      c.IsApproved.Value).ToList();
            return comments;
        }

        public IQueryable<Comment> GetComments()
        {
            return Context.Comment;
        }

        public IQueryable<Comment> GetPagedComment(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), $"{nameof(pageSize)} can not be less than 1.");
            }

            var startRow = (pageIndex - 1) * pageSize;
            var query = Context.Comment.Include(c => c.Post)
                                        .Include(c => c.CommentReply)
                                        .Where(c => c.IsApproved.Value)
                                        .OrderByDescending(p => p.CreateOnUtc)
                                        .Skip(startRow)
                                        .Take(pageSize);//.AsNoTracking();

            return query;
        }

        public Response SetApprovalStatus(Guid commentId, bool isApproved)
        {
            try
            {
                var comment = Context.Comment.Find(commentId);
                if (null != comment)
                {
                    if (isApproved)
                    {
                        Logger.LogInformation($"Approve comment {commentId}");
                        comment.IsApproved = true;
                    }
                    else
                    {
                        Logger.LogInformation($"Disapprove and delete comment {commentId}");
                        Context.Comment.Remove(comment);
                    }

                    int rows = Context.SaveChanges();
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
                var comment = Context.Comment.Find(commentId);
                if (null != comment)
                {
                    // 1. Delete all replies
                    var cReplies = Context.CommentReply.Where(cr => cr.CommentId == commentId);
                    if (cReplies.Any())
                    {
                        foreach (var commentReply in cReplies)
                        {
                            Context.Remove(commentReply);
                        }

                        Context.SaveChanges();
                    }

                    // 2. Delete comment itself
                    Context.Remove(comment);
                    var rows = Context.SaveChanges();
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

                Context.Comment.Add(model);
                int rows = Context.SaveChanges();

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

                var cmt = Context.Comment
                                  .Include(c => c.Post)
                                  .ThenInclude(p => p.PostPublish).FirstOrDefault(c => c.Id == commentId);

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
                Context.CommentReply.Add(model);
                int rows = Context.SaveChanges();

                if (rows > 0)
                {
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

                return new FailedResponse<CommentReplySummary>((int)ResponseFailureCode.DataOperationFailed);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error {nameof(NewReply)}");
                return new FailedResponse<CommentReplySummary>((int)ResponseFailureCode.GeneralException);
            }
        }
    }
}
