using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.Exporting.Exporters;
using System.Text.Json;
using Azure.Storage.Queues;
using System.Text;

namespace Moonglade.Notification.Client;

public interface IMoongladeNotification
{
    Task EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class;
}

public class MoongladeNotification : IMoongladeNotification
{
    private readonly ILogger<MoongladeNotification> _logger;
    private readonly NotificationSettings _notificationSettings;

    public MoongladeNotification(
        ILogger<MoongladeNotification> logger,
        IBlogConfig blogConfig)
    {
        _logger = logger;
        _notificationSettings = blogConfig.NotificationSettings;
    }

    public async Task EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class
    {
        if (!_notificationSettings.EnableEmailSending) return;

        try
        {
            var queue = new QueueClient(_notificationSettings.AzureStorageQueueConnection, "moongladeemailqueue");

            var uid = Guid.NewGuid();
            var en = new EmailNotificationV3
            {
                DistributionList = string.Join(';', toAddresses),
                MessageType = type.ToString(),
                MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default),
            };

            await InsertMessageAsync(queue, en);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    private async Task InsertMessageAsync(QueueClient queue, EmailNotificationV3 emailNotification)
    {
        if (null != await queue.CreateIfNotExistsAsync())
        {
            _logger.LogInformation($"Azure Storage Queue '{queue.Name}' was created.");
        }

        var json = JsonSerializer.Serialize(emailNotification);
        var bytes = Encoding.UTF8.GetBytes(json);
        var base64Json = Convert.ToBase64String(bytes);

        await queue.SendMessageAsync(base64Json);
    }
}

internal class EmailNotificationV3
{
    public string DistributionList { get; set; }
    public string MessageType { get; set; }
    public string MessageBody { get; set; }
}