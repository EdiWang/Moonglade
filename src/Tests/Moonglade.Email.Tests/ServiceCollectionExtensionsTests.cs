using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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

    [Fact]
    public void AddMoongladeEmail_EmailHelperCanLoadReadOnlyMailConfiguration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBlogConfig>(new BlogConfig());
        services.AddLogging();
        services.AddMoongladeEmail(CreateConfiguration());

        var configSource = Path.Join(AppContext.BaseDirectory, "mailConfiguration.xml");
        Assert.True(File.Exists(configSource), $"Expected email configuration file at '{configSource}'.");

        var fileInfo = new FileInfo(configSource);
        var originalAttributes = fileInfo.Attributes;
        fileInfo.Attributes = originalAttributes | FileAttributes.ReadOnly;

        try
        {
            using var serviceProvider = services.BuildServiceProvider();
            var emailHelper = serviceProvider.GetRequiredService<IEmailHelper>();

            Assert.NotNull(emailHelper);
        }
        finally
        {
            fileInfo.Attributes = originalAttributes;
        }
    }

    [Fact]
    public async Task AddMoongladeEmail_InvalidEmailServiceOptionsFailOnHostStart()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Email:Provider"] = "smtp",
                ["Email:SmtpPort"] = "587",
                ["Email:OutboxWorker:Enabled"] = "false"
            })
            .Build();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddSingleton<IBlogConfig>(new BlogConfig());
        builder.Services.AddMoongladeEmail(builder.Configuration);

        using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() =>
            host.StartAsync(TestContext.Current.CancellationToken));

        Assert.Contains("Email:SmtpServer", exception.Message);
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
