using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moonglade.Configuration;
using Moonglade.Email.Core;

namespace Moonglade.Email.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMoongladeEmail_RegistersOutboxWorkerHostedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBlogConfig>(new BlogConfig());

        services.AddMoongladeEmail(CreateConfiguration());

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(EmailOutboxWorker));
    }

    [Fact]
    public void AddMoongladeEmail_EmailSettingsUsesBlogNotificationDisplayName()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBlogConfig>(new BlogConfig
        {
            NotificationSettings = new NotificationSettings
            {
                EmailDisplayName = "Blog Sender"
            }
        });
        services.AddLogging();
        services.AddMoongladeEmail(CreateConfiguration());
        using var serviceProvider = services.BuildServiceProvider();

        var settings = serviceProvider.GetRequiredService<EmailSettings>();

        Assert.Equal("Blog Sender", settings.EmailDisplayName);
    }

    private static IConfiguration CreateConfiguration()
    {
        var values = new Dictionary<string, string>
        {
            ["Email:SmtpServer"] = "smtp.example.com",
            ["Email:SmtpUserName"] = "sender@example.com",
            ["Email:SmtpPassword"] = "password",
            ["Email:SmtpPort"] = "587"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
