using Edi.TemplateEmail;
using Edi.TemplateEmail.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Configuration;
using Moonglade.Email.Core;
using System.Collections.Concurrent;

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
    public async Task AddMoongladeEmail_MissingProviderSettingsStartHostAndLogWarning()
    {
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string>("Email:Provider", "AzureCommunication"),
            new KeyValuePair<string, string>("Email:OutboxWorker:Enabled", "false"));
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(configuration, logs);

        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var status = host.Services.GetRequiredService<EmailCapabilityStatus>();
            var options = host.Services.GetRequiredService<IOptions<EmailServiceOptions>>().Value;

            Assert.Equal(EmailCapabilityState.NotConfigured, status.State);
            Assert.Equal("AzureCommunication", options.Provider);
            Assert.Single(logs.Entries, entry =>
                entry.Level == LogLevel.Warning &&
                entry.Message.Contains("Email notifications are not configured", StringComparison.Ordinal));
            Assert.DoesNotContain(logs.Entries, entry => entry.Level == LogLevel.Error);
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AddMoongladeEmail_InvalidProviderSettingsStartHostAndLogSafeError()
    {
        const string secret = "do-not-log-this-password";
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string>("Email:SmtpPassword", secret),
            new KeyValuePair<string, string>("Email:SmtpPort", "0"),
            new KeyValuePair<string, string>("Email:OutboxWorker:Enabled", "false"));
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(configuration, logs);

        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var status = host.Services.GetRequiredService<EmailCapabilityStatus>();
            var options = host.Services.GetRequiredService<IOptions<EmailServiceOptions>>().Value;

            Assert.Equal(EmailCapabilityState.Invalid, status.State);
            Assert.Equal(0, options.SmtpPort);
            var error = Assert.Single(logs.Entries, entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains("Email notification configuration is invalid", StringComparison.Ordinal));
            Assert.Contains("Email:SmtpPort", error.Message);
            Assert.DoesNotContain(logs.Entries, entry =>
                entry.Message.Contains(secret, StringComparison.Ordinal));
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AddMoongladeEmail_InvalidWorkerSettingsStartHostAndLogError()
    {
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string>("Email:OutboxWorker:BatchSize", "0"));
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(configuration, logs);

        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var status = host.Services.GetRequiredService<EmailCapabilityStatus>();

            Assert.Equal(EmailCapabilityState.Invalid, status.State);
            var error = Assert.Single(logs.Entries, entry =>
                entry.Level == LogLevel.Error &&
                entry.Message.Contains("Email notification configuration is invalid", StringComparison.Ordinal));
            Assert.Contains("Email:OutboxWorker:BatchSize", error.Message);
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AddMoongladeEmail_DisabledWorkerStartsHostAndLogsInformation()
    {
        var configuration = CreateConfiguration(
            new KeyValuePair<string, string>("Email:OutboxWorker:Enabled", "false"));
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(configuration, logs);

        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var status = host.Services.GetRequiredService<EmailCapabilityStatus>();

            Assert.Equal(EmailCapabilityState.Disabled, status.State);
            Assert.Single(logs.Entries, entry =>
                entry.Level == LogLevel.Information &&
                entry.Message.Contains("Email notifications are disabled because", StringComparison.Ordinal));
            Assert.DoesNotContain(logs.Entries, entry => entry.Level >= LogLevel.Warning);
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task AddMoongladeEmail_ValidSettingsStartHostAndResolveEmailServices()
    {
        var logs = new CapturingLoggerProvider();
        using var host = CreateHost(CreateConfiguration(), logs, useIdleProcessor: true);

        await host.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var status = host.Services.GetRequiredService<EmailCapabilityStatus>();

            Assert.Equal(EmailCapabilityState.Available, status.State);
            Assert.NotNull(host.Services.GetRequiredService<EmailSettings>());
            Assert.NotNull(host.Services.GetRequiredService<IEmailHelper>());
            Assert.NotNull(host.Services.GetRequiredService<IEmailDispatcher>());
            Assert.Equal(2, host.Services.GetServices<IEmailProviderSender>().Count());
        }
        finally
        {
            await host.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static IHost CreateHost(
        IConfiguration configuration,
        CapturingLoggerProvider logs,
        bool useIdleProcessor = false)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(logs);
        builder.Services.AddSingleton<IBlogConfig>(new BlogConfig());
        builder.Services.AddMoongladeEmail(builder.Configuration);

        if (useIdleProcessor)
        {
            builder.Services.AddScoped<IEmailOutboxMessageProcessor, IdleEmailOutboxMessageProcessor>();
        }

        return builder.Build();
    }

    private static IConfiguration CreateConfiguration(
        params KeyValuePair<string, string>[] overrides)
    {
        var values = new Dictionary<string, string>
        {
            ["Email:SmtpServer"] = "smtp.example.com",
            ["Email:SmtpUserName"] = "sender@example.com",
            ["Email:SmtpPassword"] = "password",
            ["Email:SmtpPort"] = "587"
        };

        foreach (var item in overrides)
        {
            values[item.Key] = item.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class IdleEmailOutboxMessageProcessor : IEmailOutboxMessageProcessor
    {
        public Task<bool> ProcessNextAsync(string workerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

        public IReadOnlyCollection<CapturedLogEntry> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<CapturedLogEntry> entries) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                entries.Enqueue(new(logLevel, formatter(state, exception)));
            }
        }
    }

    private sealed record CapturedLogEntry(LogLevel Level, string Message);
}
