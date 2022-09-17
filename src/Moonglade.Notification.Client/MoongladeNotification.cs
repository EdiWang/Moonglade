using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Exporting.Exporters;
using Moonglade.Data.Infrastructure;
using System.Text.Json;

namespace Moonglade.Notification.Client;

public interface IMoongladeNotification
{
    Task<Guid> EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class;
}

public class MoongladeNotification : IMoongladeNotification
{
    private readonly bool _isEnabled;
    private readonly ILogger<MoongladeNotification> _logger;
    private readonly IRepository<EmailNotificationEntity> _repo;

    public MoongladeNotification(
        ILogger<MoongladeNotification> logger,
        IRepository<EmailNotificationEntity> repo,
        IBlogConfig blogConfig)
    {
        _logger = logger;
        _repo = repo;
        _isEnabled = blogConfig.NotificationSettings.EnableEmailSending;
    }

    public async Task<Guid> EnqueueNotification<T>(MailMesageTypes type, string[] toAddresses, T payload) where T : class
    {
        if (!_isEnabled) return Guid.Empty;

        try
        {
            var uid = Guid.NewGuid();
            var en = new EmailNotificationEntity
            {
                Id = uid,
                DistributionList = string.Join(';', toAddresses),
                MessageType = type.ToString(),
                MessageBody = JsonSerializer.Serialize(payload, MoongladeJsonSerializerOptions.Default),
                CreateTimeUtc = DateTime.UtcNow,
                SendingStatus = 1,
                RetryCount = 0
            };

            await _repo.AddAsync(en);
            return uid;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }
}