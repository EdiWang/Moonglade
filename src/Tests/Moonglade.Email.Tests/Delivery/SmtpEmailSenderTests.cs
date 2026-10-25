using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moonglade.Email.Core;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Moonglade.Email.Tests.Delivery;

public class SmtpEmailSenderTests
{
    [Theory]
    [InlineData(465, true, SecureSocketOptions.SslOnConnect)]
    [InlineData(465, false, SecureSocketOptions.SslOnConnect)]
    [InlineData(587, true, SecureSocketOptions.StartTls)]
    [InlineData(587, false, SecureSocketOptions.Auto)]
    [InlineData(25, true, SecureSocketOptions.StartTls)]
    [InlineData(25, false, SecureSocketOptions.Auto)]
    [InlineData(2525, true, SecureSocketOptions.StartTls)]
    [InlineData(2525, false, SecureSocketOptions.Auto)]
    public void GetSocketOptions_SelectsTransportSecurity(int port, bool enableTls, SecureSocketOptions expected)
    {
        Assert.Equal(expected, SmtpEmailSender.GetSocketOptions(port, enableTls));
    }

    [Theory]
    [InlineData("resend", "notifications@example.com", true)]
    [InlineData("resend", "notifications@example.com", false)]
    [InlineData("legacy@example.com", "", true)]
    [InlineData("legacy@example.com", null, false)]
    [InlineData("legacy@example.com", "   ", false)]
    public async Task SendAsync_SeparatesAuthenticationFromSenderAndPreservesMessage(
        string userName,
        string senderAddress,
        bool isHtml)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var settings = new EmailSettings
        {
            EmailDisplayName = "Blog Sender",
            SmtpSettings = new SmtpSettings("127.0.0.1", userName, "test-password", port)
            {
                EnableTls = false
            }
        };
        var sender = new SmtpEmailSender(
            settings,
            Options.Create(new EmailServiceOptions
            {
                SmtpUserName = userName,
                SmtpSenderAddress = senderAddress
            }),
            NullLogger<SmtpEmailSender>.Instance);
        var message = new CommonMailMessage
        {
            Subject = "SMTP sender test",
            Body = isHtml ? "<p>Hello</p>" : "Hello",
            BodyIsHtml = isHtml,
            Receipts = ["reader@example.com", "second@example.com"],
            CcReceipts = ["copy@example.com"]
        };

        var serverTask = ReceiveMessageAsync(listener, timeout.Token);
        await Task.WhenAll(sender.SendAsync(message).WaitAsync(timeout.Token), serverTask);
        var delivery = await serverTask;
        var effectiveAddress = string.IsNullOrWhiteSpace(senderAddress) ? userName : senderAddress;
        var from = Assert.Single(delivery.Message.From.Mailboxes);

        Assert.Equal(userName, delivery.UserName);
        Assert.Equal("test-password", delivery.Password);
        Assert.Contains($"MAIL FROM:<{effectiveAddress}>", delivery.EnvelopeCommands);
        Assert.Contains("RCPT TO:<reader@example.com>", delivery.EnvelopeCommands);
        Assert.Contains("RCPT TO:<second@example.com>", delivery.EnvelopeCommands);
        Assert.Contains("RCPT TO:<copy@example.com>", delivery.EnvelopeCommands);
        Assert.Equal(effectiveAddress, from.Address);
        Assert.Equal("Blog Sender", from.Name);
        Assert.Equal(effectiveAddress, delivery.Message.Sender.Address);
        Assert.Equal(message.Subject, delivery.Message.Subject);
        Assert.Equal(message.Receipts, delivery.Message.To.Mailboxes.Select(address => address.Address));
        Assert.Equal(message.CcReceipts, delivery.Message.Cc.Mailboxes.Select(address => address.Address));
        Assert.Equal(message.Body, (isHtml ? delivery.Message.HtmlBody : delivery.Message.TextBody).TrimEnd('\r', '\n'));
    }

    private static async Task<SmtpDelivery> ReceiveMessageAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        using var connection = await listener.AcceptTcpClientAsync(cancellationToken);
        await using var stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };
        var envelopeCommands = new List<string>();
        string userName = null;
        string password = null;
        MimeMessage message = null;
        await writer.WriteLineAsync("220 localhost SMTP test server".AsMemory(), cancellationToken);

        while (await reader.ReadLineAsync(cancellationToken) is { } command)
        {
            string response;
            if (command.StartsWith("EHLO ", StringComparison.Ordinal))
            {
                response = "250-localhost\r\n250 AUTH PLAIN";
            }
            else if (command.StartsWith("AUTH PLAIN ", StringComparison.Ordinal))
            {
                var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(command[11..])).Split('\0');
                userName = credentials[1];
                password = credentials[2];
                response = "235 Authentication successful";
            }
            else if (command.StartsWith("MAIL FROM:", StringComparison.Ordinal) ||
                     command.StartsWith("RCPT TO:", StringComparison.Ordinal))
            {
                envelopeCommands.Add(command);
                response = "250 OK";
            }
            else if (command == "DATA")
            {
                await writer.WriteLineAsync("354 End data with <CRLF>.<CRLF>".AsMemory(), cancellationToken);
                var data = new StringBuilder();
                while (await reader.ReadLineAsync(cancellationToken) is { } line && line != ".")
                {
                    data.Append(line.StartsWith('.') ? line[1..] : line).Append("\r\n");
                }

                using var messageStream = new MemoryStream(Encoding.UTF8.GetBytes(data.ToString()));
                message = await MimeMessage.LoadAsync(messageStream, cancellationToken);
                response = "250 Message accepted";
            }
            else if (command == "QUIT")
            {
                await writer.WriteLineAsync("221 Goodbye".AsMemory(), cancellationToken);
                return new(userName, password, envelopeCommands, message);
            }
            else
            {
                throw new InvalidOperationException($"Unexpected SMTP command: {command}");
            }

            await writer.WriteLineAsync(response.AsMemory(), cancellationToken);
        }

        throw new InvalidOperationException("SMTP connection closed before QUIT.");
    }

    private sealed record SmtpDelivery(
        string UserName,
        string Password,
        IReadOnlyList<string> EnvelopeCommands,
        MimeMessage Message);
}
