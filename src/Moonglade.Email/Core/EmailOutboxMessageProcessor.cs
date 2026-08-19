using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Moonglade.Email.Core;

public class EmailOutboxMessageProcessor(
    IEmailOutboxStore outboxStore,
    MessageBuilder messageBuilder,
    EmailSettings emailSettings,
    IEmailDispatcher dispatcher,
    IOptions<EmailOutboxWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<EmailOutboxMessageProcessor> logger) : IEmailOutboxMessageProcessor
{
    public async Task<bool> ProcessNextAsync(string workerId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var message = await outboxStore.ClaimNextAsync(
            new EmailOutboxClaimRequest(workerId, utcNow, options.Value.LeaseDuration),
            cancellationToken);

        if (message == null)
        {
            return false;
        }

        await ProcessClaimedMessageAsync(message, utcNow, cancellationToken);
        return true;
    }

    private async Task ProcessClaimedMessageAsync(
        EmailOutboxMessage message,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateMessage(message);
        if (validationErrors.Length > 0)
        {
            await outboxStore.DeadLetterAsync(
                new EmailOutboxFailure(message.Id, string.Join("; ", validationErrors), utcNow),
                cancellationToken);
            return;
        }

        var recipients = EmailNotificationContract.ParseDistributionList(message.DistributionList);
        var failures = await SendToRecipientsAsync(message, recipients, cancellationToken);

        if (failures.Count == 0)
        {
            await outboxStore.CompleteAsync(message.Id, utcNow, cancellationToken);
            return;
        }

        if (failures.Count != recipients.Length)
        {
            logger.LogWarning(
                "Email outbox message {MessageId} completed with {FailureCount} failed recipients out of {RecipientCount}.",
                message.Id,
                failures.Count,
                recipients.Length);

            await outboxStore.CompleteAsync(message.Id, utcNow, cancellationToken);
            return;
        }

        var transientFailures = failures.Count(f => f.Kind == EmailDeliveryFailureKind.Transient);
        var errorMessage = BuildFailureMessage(failures);

        if (transientFailures == 0)
        {
            await outboxStore.DeadLetterAsync(
                new EmailOutboxFailure(message.Id, errorMessage, utcNow),
                cancellationToken);
            return;
        }

        if (message.AttemptCount >= options.Value.MaxAttempts)
        {
            await outboxStore.DeadLetterAsync(
                new EmailOutboxFailure(message.Id, $"Max retry attempts reached. {errorMessage}", utcNow),
                cancellationToken);
            return;
        }

        await outboxStore.MarkFailedAsync(
            new EmailOutboxFailure(message.Id, errorMessage, utcNow),
            utcNow.Add(GetRetryDelay(message.AttemptCount)),
            cancellationToken);
    }

    private async Task<IReadOnlyList<EmailDeliveryFailure>> SendToRecipientsAsync(
        EmailOutboxMessage message,
        string[] recipients,
        CancellationToken cancellationToken)
    {
        var failures = new List<EmailDeliveryFailure>();

        foreach (var recipient in recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var emailMessage = BuildMessage(message, [recipient]);
                await dispatcher.SendAsync(emailMessage);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var classified = EmailDeliveryFailureClassifier.TryClassify(ex, out var kind);
                if (!classified)
                {
                    kind = EmailDeliveryFailureKind.Transient;
                }

                failures.Add(new EmailDeliveryFailure(recipient, kind, ex));
                logger.LogError(
                    ex,
                    "{FailureKind} error sending outbox message {MessageId} to {Recipient}.",
                    kind,
                    message.Id,
                    recipient);
            }
        }

        return failures;
    }

    private CommonMailMessage BuildMessage(EmailOutboxMessage message, string[] recipients)
    {
        return message.MessageType switch
        {
            MessageTypes.TestMail => messageBuilder.BuildTestNotification(recipients, emailSettings),
            MessageTypes.NewCommentNotification => messageBuilder.BuildNewCommentNotification(
                recipients,
                JsonSerializer.Deserialize<NewCommentPayload>(message.MessageBody, MoongladeJsonSerializerOptions.Default)),
            MessageTypes.AdminReplyNotification => messageBuilder.BuildCommentReplyNotification(
                recipients,
                JsonSerializer.Deserialize<CommentReplyPayload>(message.MessageBody, MoongladeJsonSerializerOptions.Default)),
            MessageTypes.BeingPinged => messageBuilder.BuildPingNotification(
                recipients,
                JsonSerializer.Deserialize<PingPayload>(message.MessageBody, MoongladeJsonSerializerOptions.Default)),
            _ => throw new ArgumentOutOfRangeException(nameof(message.MessageType), message.MessageType, "Unsupported message type.")
        };
    }

    private static string[] ValidateMessage(EmailOutboxMessage message)
    {
        var notification = new EmailNotification
        {
            DistributionList = message.DistributionList,
            MessageType = message.MessageType,
            MessageBody = message.MessageBody
        };

        var errors = EmailNotificationContract.ValidateNotification(notification);
        if (errors.Length == 0)
        {
            errors = EmailNotificationContract.ValidatePayload(notification.MessageType, notification.MessageBody);
        }

        return errors;
    }

    private TimeSpan GetRetryDelay(int attemptCount)
    {
        var delay = options.Value.InitialRetryDelay;

        for (var i = 1; i < attemptCount; i++)
        {
            delay = delay + delay;
            if (delay >= options.Value.MaxRetryDelay)
            {
                return options.Value.MaxRetryDelay;
            }
        }

        return delay;
    }

    private static string BuildFailureMessage(IEnumerable<EmailDeliveryFailure> failures)
    {
        return string.Join(
            "; ",
            failures.Select(f => $"{f.Recipient}: {f.Kind} - {f.Exception.Message}"));
    }
}
