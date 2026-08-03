using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Xml.Serialization;

namespace Moonglade.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMoongladeEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<EmailServiceOptions>()
            .Bind(configuration.GetSection("Email"));

        services.AddSingleton<IValidateOptions<EmailOutboxWorkerOptions>, EmailOutboxWorkerOptionsValidator>();
        services.AddOptions<EmailOutboxWorkerOptions>()
            .Bind(configuration.GetSection(EmailOutboxWorkerOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IEmailHelper>(_ => new EmailHelper(LoadMailConfiguration()));

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EmailServiceOptions>>().Value;
            var blogConfig = sp.GetRequiredService<IBlogConfig>();
            var smtpSettings = new SmtpSettings(opts.SmtpServer, opts.SmtpUserName, opts.SmtpPassword, opts.SmtpPort)
            {
                EnableTls = opts.EnableSsl
            };
            var settings = new EmailSettings { SmtpSettings = smtpSettings };
            var senderDisplayName = blogConfig.NotificationSettings.EmailDisplayName;
            if (string.IsNullOrWhiteSpace(senderDisplayName))
            {
                senderDisplayName = opts.SenderDisplayName;
            }

            if (!string.IsNullOrWhiteSpace(senderDisplayName))
            {
                settings.EmailDisplayName = senderDisplayName;
            }

            return settings;
        });

        services.AddSingleton<MessageBuilder>();
        services.AddSingleton<IAzureCommunicationEmailClient, AzureCommunicationEmailClient>();
        services.AddSingleton<IEmailProviderSender, SmtpEmailSender>();
        services.AddSingleton<IEmailProviderSender, AzureCommunicationSender>();
        services.AddSingleton<IEmailDispatcher, EmailDispatcher>();

        services.AddScoped<DbEmailNotificationQueue>();
        services.AddScoped<IEmailOutboxStore>(sp => sp.GetRequiredService<DbEmailNotificationQueue>());
        services.AddScoped<IEmailNotificationQueue>(sp => sp.GetRequiredService<DbEmailNotificationQueue>());
        services.AddScoped<IEmailOutboxMessageProcessor, EmailOutboxMessageProcessor>();
        services.AddHostedService<EmailOutboxWorker>();

        return services;
    }

    private static MailConfiguration LoadMailConfiguration()
    {
        var configSource = Path.Join(AppContext.BaseDirectory, "mailConfiguration.xml");
        if (!File.Exists(configSource))
        {
            configSource = Path.Join(AppContext.BaseDirectory, "Moonglade.Email", "mailConfiguration.xml");
        }

        if (!File.Exists(configSource))
        {
            throw new FileNotFoundException("Configuration file for EmailHelper is not present.", configSource);
        }

        var serializer = new XmlSerializer(typeof(MailConfiguration));
        using var stream = File.Open(configSource, FileMode.Open, FileAccess.Read, FileShare.Read);

        return serializer.Deserialize(stream) as MailConfiguration
            ?? throw new InvalidOperationException("Configuration file for EmailHelper is invalid.");
    }
}
