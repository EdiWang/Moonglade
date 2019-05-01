using System;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using Edi.Practice.RequestResponseModel;
using Edi.TemplateEmail.NetStd;
using Microsoft.AspNetCore.Hosting;
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

        private readonly IHostingEnvironment _env;

        private IEmailHelper EmailHelper { get; }

        private readonly BlogConfig _blogConfig;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<AppSettings> settings,
            IHostingEnvironment env,
            BlogConfig blogConfig,
            BlogConfigurationService blogConfigurationService,
            IRepository<Post> postRepository) : base(logger, settings)
        {
            _env = env;
            _blogConfig = blogConfig;
            _postRepository = postRepository;
            _blogConfig.Initialize(blogConfigurationService);

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
                    _blogConfig.EmailConfiguration.SmtpClearPassword,
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

                var pipeline = new TemplatePipeline().Map(nameof(Environment.MachineName), Environment.MachineName)
                                                     .Map(nameof(EmailHelper.Settings.SmtpServer), EmailHelper.Settings.SmtpServer)
                                                     .Map(nameof(EmailHelper.Settings.SmtpServerPort), EmailHelper.Settings.SmtpServerPort)
                                                     .Map(nameof(EmailHelper.Settings.SmtpUserName), EmailHelper.Settings.SmtpUserName)
                                                     .Map(nameof(EmailHelper.Settings.EmailDisplayName), EmailHelper.Settings.EmailDisplayName)
                                                     .Map(nameof(EmailHelper.Settings.EnableSsl), EmailHelper.Settings.EnableSsl);
                if (_blogConfig.EmailConfiguration.EnableEmailSending && !BlockEmailSending)
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

            var pipeline = new TemplatePipeline().Map(nameof(comment.Username), comment.Username)
                                                 .Map(nameof(comment.Email), comment.Email)
                                                 .Map(nameof(comment.IPAddress), comment.IPAddress)
                                                 .Map(nameof(comment.CreateOnUtc), comment.CreateOnUtc.ToString("MM/dd/yyyy HH:mm"))
                                                 .Map("Title", postTitle)
                                                 .Map(nameof(comment.CommentContent), comment.CommentContent);

            if (_blogConfig.EmailConfiguration.EnableEmailSending && !BlockEmailSending)
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

            if (model.PubDateUtc != null)
            {
                Logger.LogInformation("Sending AdminReplyNotification mail");

                var pipeline = new TemplatePipeline().Map("ReplyTime",
                                                         Utils.UtcToZoneTime(model.ReplyTimeUtc.GetValueOrDefault(), AppSettings.TimeZone))
                                                     .Map(nameof(model.ReplyContent), model.ReplyContent)
                                                     .Map("RouteLink", postLink)
                                                     .Map("PostTitle", model.Title)
                                                     .Map(nameof(model.CommentContent), model.CommentContent);

                if (_blogConfig.EmailConfiguration.EnableEmailSending && !BlockEmailSending)
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
                                                     .Map(nameof(receivedPingback.SourceIp), receivedPingback.SourceIp)
                                                     .Map(nameof(receivedPingback.SourceTitle), receivedPingback.SourceTitle)
                                                     .Map(nameof(receivedPingback.SourceUrl), receivedPingback.SourceUrl)
                                                     .Map(nameof(receivedPingback.Direction), receivedPingback.Direction);

                if (_blogConfig.EmailConfiguration.EnableEmailSending && !BlockEmailSending)
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

        private bool BlockEmailSending =>
             _env.IsDevelopment() && AppSettings.DisableEmailSendingInDevelopment;
    }
}
