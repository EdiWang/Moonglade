using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;

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

        throw new NotImplementedException();
    }
}