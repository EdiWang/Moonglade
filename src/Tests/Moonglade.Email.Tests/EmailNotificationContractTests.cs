using Moonglade.Email.Core;
using System.Text.Json;

namespace Moonglade.Email.Tests;

public class EmailNotificationContractTests
{
    [Fact]
    public void ValidateMessageType_SupportedType_ReturnsNoErrors()
    {
        var errors = EmailNotificationContract.ValidateMessageType(MessageTypes.NewCommentNotification);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateMessageType_UnknownType_ReturnsError()
    {
        var errors = EmailNotificationContract.ValidateMessageType("Unknown");

        Assert.Contains(errors, error => error.Contains("not supported"));
    }

    [Fact]
    public void ValidateRecipients_ValidRecipients_ReturnsNoErrors()
    {
        var errors = EmailNotificationContract.ValidateRecipients(["admin@example.com", "user@example.com"]);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateRecipients_InvalidRecipient_ReturnsError()
    {
        var errors = EmailNotificationContract.ValidateRecipients(["not-an-email"]);

        Assert.Contains(errors, error => error.Contains("invalid"));
    }

    [Fact]
    public void ValidateRecipients_TooManyRecipients_ReturnsError()
    {
        var recipients = Enumerable.Range(0, EmailNotificationContract.MaxRecipients + 1)
            .Select(index => $"user{index}@example.com")
            .ToArray();

        var errors = EmailNotificationContract.ValidateRecipients(recipients);

        Assert.Contains(errors, error => error.Contains("must not exceed"));
    }

    [Fact]
    public void ParseDistributionList_TrimsEmptyEntries()
    {
        var recipients = EmailNotificationContract.ParseDistributionList(" admin@example.com ; ; user@example.com ");

        Assert.Equal(["admin@example.com", "user@example.com"], recipients);
    }

    [Fact]
    public void ValidatePayload_NewCommentWithRequiredFields_ReturnsNoErrors()
    {
        var payload = new NewCommentPayload
        {
            Username = "John",
            Email = "john@example.com",
            IpAddress = "127.0.0.1",
            PostTitle = "Hello",
            CommentContent = "Great post"
        };
        var json = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default);

        var errors = EmailNotificationContract.ValidatePayload(MessageTypes.NewCommentNotification, json);

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidatePayload_NewCommentWithMissingEmail_ReturnsError()
    {
        var payload = new NewCommentPayload
        {
            Username = "John",
            IpAddress = "127.0.0.1",
            PostTitle = "Hello",
            CommentContent = "Great post"
        };
        var json = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default);

        var errors = EmailNotificationContract.ValidatePayload(MessageTypes.NewCommentNotification, json);

        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ValidatePayload_InvalidJson_ReturnsError()
    {
        var errors = EmailNotificationContract.ValidatePayload(MessageTypes.NewCommentNotification, "{");

        Assert.Contains("Payload is not valid JSON", errors);
    }
}
