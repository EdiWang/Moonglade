using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Moonglade.Email.Core;

public static class EmailNotificationContract
{
    public const int MaxRecipients = 20;
    public const int MaxEmailAddressLength = 320;

    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public static readonly string[] SupportedMessageTypes =
    [
        MessageTypes.TestMail,
        MessageTypes.NewCommentNotification,
        MessageTypes.AdminReplyNotification,
        MessageTypes.BeingPinged
    ];

    public static bool IsSupportedMessageType(string messageType) =>
        !string.IsNullOrWhiteSpace(messageType) &&
        SupportedMessageTypes.Contains(messageType, StringComparer.Ordinal);

    public static string[] ValidateMessageType(string messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType))
        {
            return ["Type is required"];
        }

        if (!IsSupportedMessageType(messageType))
        {
            return [$"Type '{messageType}' is not supported"];
        }

        return [];
    }

    public static string[] ParseDistributionList(string distributionList) =>
        (distributionList ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static string[] ValidateRecipients(IEnumerable<string> recipients)
    {
        var errors = new List<string>();
        var recipientList = recipients?.ToArray() ?? [];

        if (recipientList.Length == 0)
        {
            errors.Add("At least one recipient is required");
            return [.. errors];
        }

        if (recipientList.Length > MaxRecipients)
        {
            errors.Add($"Recipients must not exceed {MaxRecipients} addresses");
        }

        foreach (var recipient in recipientList)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                errors.Add("Recipient address cannot be empty");
                continue;
            }

            if (recipient.Length > MaxEmailAddressLength)
            {
                errors.Add($"Recipient address '{recipient}' must not exceed {MaxEmailAddressLength} characters");
                continue;
            }

            if (!EmailAddressAttribute.IsValid(recipient))
            {
                errors.Add($"Recipient address '{recipient}' is invalid");
            }
        }

        return [.. errors];
    }

    public static string[] ValidateNotification(EmailNotification notification)
    {
        if (notification == null)
        {
            return ["Message could not be deserialized"];
        }

        return
        [
            .. ValidateRecipients(ParseDistributionList(notification.DistributionList)),
            .. ValidateMessageType(notification.MessageType)
        ];
    }

    public static string[] ValidatePayload(string messageType, string messageBody)
    {
        try
        {
            return messageType switch
            {
                MessageTypes.TestMail => [],
                MessageTypes.NewCommentNotification => ValidateTypedPayload<NewCommentPayload>(messageBody),
                MessageTypes.AdminReplyNotification => ValidateTypedPayload<CommentReplyPayload>(messageBody),
                MessageTypes.BeingPinged => ValidateTypedPayload<PingPayload>(messageBody),
                _ => [$"Type '{messageType}' is not supported"]
            };
        }
        catch (JsonException)
        {
            return ["Payload is not valid JSON"];
        }
    }

    private static string[] ValidateTypedPayload<TPayload>(string messageBody)
        where TPayload : class
    {
        if (string.IsNullOrWhiteSpace(messageBody))
        {
            return ["Payload is required"];
        }

        var payload = JsonSerializer.Deserialize<TPayload>(messageBody, MoongladeJsonSerializerOptions.Default);
        if (payload == null)
        {
            return ["Payload is required"];
        }

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(payload);
        if (Validator.TryValidateObject(payload, validationContext, validationResults, true))
        {
            return [];
        }

        return validationResults
            .Select(result => result.ErrorMessage ?? "Payload is invalid")
            .ToArray();
    }
}
