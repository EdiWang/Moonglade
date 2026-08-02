using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Moonglade.Email;

namespace Moonglade.Email.Core;

public class MessageBuilder(IEmailHelper emailHelper)
{
    public CommonMailMessage BuildTestNotification(string[] toAddresses, EmailSettings smtpSettings)
    {
        var message = emailHelper.ForType(MessageTypes.TestMail)
            .Map(nameof(Environment.MachineName), Environment.MachineName)
            .Map(nameof(SmtpSettings.SmtpServer), smtpSettings.SmtpSettings.SmtpServer)
            .Map(nameof(SmtpSettings.SmtpServerPort), smtpSettings.SmtpSettings.SmtpServerPort)
            .Map(nameof(SmtpSettings.SmtpUserName), smtpSettings.SmtpSettings.SmtpUserName)
            .Map(nameof(EmailSettings.EmailDisplayName), smtpSettings.EmailDisplayName)
            .Map(nameof(SmtpSettings.EnableTls), smtpSettings.SmtpSettings.EnableTls)
            .BuildMessage(toAddresses);

        return message;
    }

    public CommonMailMessage BuildNewCommentNotification(string[] toAddresses, NewCommentPayload request)
    {
        var message = emailHelper.ForType(MessageTypes.NewCommentNotification)
            .Map(nameof(request.Username), request.Username)
            .Map(nameof(request.Email), request.Email)
            .Map(nameof(request.IpAddress), request.IpAddress)
            .Map(nameof(request.PostTitle), request.PostTitle)
            .Map(nameof(request.CommentContent), request.CommentContent)
            .BuildMessage(toAddresses);

        return message;
    }

    public CommonMailMessage BuildCommentReplyNotification(string[] toAddress, CommentReplyPayload request)
    {
        var message = emailHelper.ForType(MessageTypes.AdminReplyNotification)
            .Map(nameof(request.ReplyContentHtml), request.ReplyContentHtml)
            .Map(nameof(request.PostLink), request.PostLink)
            .Map(nameof(request.Title), request.Title)
            .Map(nameof(request.CommentContent), request.CommentContent)
            .BuildMessage(toAddress);

        return message;
    }

    public CommonMailMessage BuildPingNotification(string[] toAddresses, PingPayload request)
    {
        var message = emailHelper.ForType(MessageTypes.BeingPinged)
            .Map(nameof(request.TargetPostTitle), request.TargetPostTitle)
            .Map(nameof(request.Domain), request.Domain)
            .Map(nameof(request.SourceIp), request.SourceIp)
            .Map(nameof(request.SourceTitle), request.SourceTitle)
            .Map(nameof(request.SourceUrl), request.SourceUrl)
            .BuildMessage(toAddresses);

        return message;
    }
}
