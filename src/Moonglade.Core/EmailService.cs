using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Edi.TemplateEmail.NetStd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Model;
using Moonglade.Model.Settings;

namespace Moonglade.Core
{
    public class EmailService : MoongladeService
    {
        private readonly IRepository<Post> _postRepository;

        private EmailHelper EmailHelper { get; }

        private readonly BlogConfig _blogConfig;

        public EmailService(
            ILogger<EmailService> logger, 
            IOptions<AppSettings> settings,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            IRepository<Post> postRepository) : base(logger: logger, settings: settings)
        {
            _blogConfig = blogConfig;
            _postRepository = postRepository;
            _blogConfig.GetConfiguration(blogConfigurationService);

            var configSource = $@"{AppDomain.CurrentDomain.GetData(Constants.AppBaseDirectory)}\mailConfiguration.xml";
            if (!File.Exists(configSource))
            {
                throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
            }

            if (EmailHelper == null)
            {
                var emailSettings = new EmailSettings(
                    _blogConfig.EmailConfiguration.SmtpServer,
                    _blogConfig.EmailConfiguration.SmtpUserName,
                    _blogConfig.EmailConfiguration.SmtpPassword,
                    _blogConfig.EmailConfiguration.SmtpServerPort)
                {
                    EnableSsl = _blogConfig.EmailConfiguration.EnableSsl,
                    EmailDisplayName = _blogConfig.EmailConfiguration.EmailDisplayName,
                    SenderName = _blogConfig.EmailConfiguration.EmailDisplayName
                };

                EmailHelper = new EmailHelper(configSource, emailSettings);
                EmailHelper.EmailSent += (sender, eventArgs) =>
                {
                    if (sender is MailMessage msg)
                    {
                        Logger.LogInformation($"Email {msg.Subject} is sent, Success: {eventArgs.IsSuccess}");
                    }
                };
            }
        }

        public async Task<Response> TestSendTestMailAsync()
        {
            try
            {
                Logger.LogInformation("Sending test mail");

                var pipeline = new TemplatePipeline().Map("MachineName", Environment.MachineName)
                                                     .Map("SmtpServer", EmailHelper.Settings.SmtpServer)
                                                     .Map("SmtpServerPort", EmailHelper.Settings.SmtpServerPort)
                                                     .Map("SmtpUserName", EmailHelper.Settings.SmtpUserName)
                                                     .Map("EmailDisplayName", EmailHelper.Settings.EmailDisplayName)
                                                     .Map("EnableSsl", EmailHelper.Settings.EnableSsl);
                if (_blogConfig.EmailConfiguration.EnableEmailSending)
                {
                    await EmailHelper.ApplyTemplate(MailMesageType.TestMail, pipeline)
                                     .SendMailAsync(_blogConfig.EmailConfiguration.AdminEmail);

                    return new SuccessResponse();
                }

                return new FailedResponse((int)ResponseFailureCode.EmailSendingDisabled);
            }
            catch (Exception e)
            {
                Logger.LogError(e, nameof(TestSendTestMailAsync));
                return new FailedResponse((int)ResponseFailureCode.GeneralException)
                {
                    Exception = e,
                    Message = e.Message
                };
            }
        }

        public async Task SendNewCommentNotificationAsync(Comment comment, string postTitle)
        {
            Logger.LogInformation("Sending NewCommentNotification mail");

            var pipeline = new TemplatePipeline().Map("Username", comment.Username)
                                                 .Map("Email", comment.Email)
                                                 .Map("IPAddress", comment.IPAddress)
                                                 .Map("PubDateUtc", comment.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
                                                 .Map("Title", postTitle)
                                                 .Map("CommentContent", comment.CommentContent);

            if (_blogConfig.EmailConfiguration.EnableEmailSending)
            {
                await EmailHelper.ApplyTemplate(MailMesageType.NewCommentNotification, pipeline)
                                 .SendMailAsync(_blogConfig.EmailConfiguration.AdminEmail);
            }
        }

        public async Task SendCommentReplyNotification(CommentReplySummary model, string postLink)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                return;
            }

            if (model.PubDateUTC != null)
            {
                Logger.LogInformation("Sending AdminReplyNotification mail");

                var pipeline = new TemplatePipeline().Map("ReplyTime",
                                                         Utils.UtcToZoneTime(model.ReplyTimeUtc.GetValueOrDefault(), AppSettings.TimeZone))
                                                     .Map("ReplyContent", model.ReplyContent)
                                                     .Map("RouteLink", postLink)
                                                     .Map("PostTitle", model.Title)
                                                     .Map("CommentContent", model.CommentContent);

                if (_blogConfig.EmailConfiguration.EnableEmailSending)
                {
                    await EmailHelper.ApplyTemplate(MailMesageType.AdminReplyNotification, pipeline)
                                     .SendMailAsync(model.Email);
                }
            }
        }

        public async Task SendPingNotification(PingbackHistory receivedPingback)
        {
            var post = _postRepository.Get(receivedPingback.TargetPostId);
            if (null != post)
            {
                Logger.LogInformation($"Sending BeingPinged mail for post id {receivedPingback.TargetPostId}");

                var postTitle = post.Title;
                var pipeline = new TemplatePipeline().Map("Title", postTitle)
                                                     .Map("PingTime", receivedPingback.PingTimeUtc)
                                                     .Map("SourceDomain", receivedPingback.Domain)
                                                     .Map("SourceIp", receivedPingback.SourceIp)
                                                     .Map("SourceTitle", receivedPingback.SourceTitle)
                                                     .Map("SourceUrl", receivedPingback.SourceUrl)
                                                     .Map("Direction", receivedPingback.Direction);

                if (_blogConfig.EmailConfiguration.EnableEmailSending)
                {
                    await EmailHelper.ApplyTemplate(MailMesageType.BeingPinged, pipeline)
                        .SendMailAsync(_blogConfig.EmailConfiguration.AdminEmail);
                }
            }
            else
            {
                Logger.LogWarning($"Post id {receivedPingback.TargetPostId} not found, skipping sending ping notification email.");
            }
        }
    }
}
