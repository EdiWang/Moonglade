using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.Exporting.Exporters;
using System.Text;
using System.Text.Json;

namespace Moonglade.Email.Client;

public interface IBlogNotification
{
    Task Enqueue<T>(MailMesageTypes type, string[] receipts, T payload) where T : class;
}

public class AzureStorageQueueNotification(ILogger<AzureStorageQueueNotification> logger,
        IBlogConfig blogConfig)
    : IBlogNotification
{
    private readonly NotificationSettings _notificationSettings = blogConfig.NotificationSettings;

    public async Task Enqueue<T>(MailMesageTypes type, string[] receipts, T payload) where T : class
    {
        if (!_notificationSettings.EnableEmailSending) return;

        try
        {
            var queue = new QueueClient(_notificationSettings.AzureStorageQueueConnection, "moongladeemailqueue");

            var en = new EmailNotification
            {
                DistributionList = string.Join(';', receipts),
                MessageType = type.ToString(),
                MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default),
            };

            await InsertMessageAsync(queue, en);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            throw;
        }
    }

    private async Task InsertMessageAsync(QueueClient queue, EmailNotification emailNotification)
    {
        if (null != await queue.CreateIfNotExistsAsync())
        {
            logger.LogInformation($"Azure Storage Queue '{queue.Name}' was created.");
        }

        var json = JsonSerializer.Serialize(emailNotification);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(bytes);

        await queue.SendMessageAsync(base64Json);
    }
}