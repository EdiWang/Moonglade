using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Email.Core;
using Moq;

namespace Moonglade.Email.Tests;

public class EmailOutboxWorkerTests
{
    [Theory]
    [InlineData(EmailCapabilityState.NotConfigured)]
    [InlineData(EmailCapabilityState.Invalid)]
    [InlineData(EmailCapabilityState.Disabled)]
    public async Task ExecuteAsync_WhenEmailCapabilityUnavailable_DoesNotCreateScopeOrPollOutbox(
        EmailCapabilityState state)
    {
        var status = CreateStatus(state, out var workerOptions);
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var worker = new TestableEmailOutboxWorker(
            scopeFactory.Object,
            Options.Create(workerOptions),
            status,
            Mock.Of<ILogger<EmailOutboxWorker>>());

        await worker.RunAsync(TestContext.Current.CancellationToken);

        Assert.False(status.IsAvailable);
        scopeFactory.Verify(factory => factory.CreateScope(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailCapabilityAvailable_PollsOutbox()
    {
        using var cancellationSource = new CancellationTokenSource();
        var processor = new CancellingEmailOutboxMessageProcessor(cancellationSource);
        var services = new ServiceCollection();
        services.AddScoped<IEmailOutboxMessageProcessor>(_ => processor);
        using var serviceProvider = services.BuildServiceProvider();

        var status = CreateStatus(EmailCapabilityState.Available, out var workerOptions);
        var worker = new TestableEmailOutboxWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(workerOptions),
            status,
            Mock.Of<ILogger<EmailOutboxWorker>>());

        await worker.RunAsync(cancellationSource.Token);

        Assert.Equal(1, processor.CallCount);
    }

    private static EmailCapabilityStatus CreateStatus(
        EmailCapabilityState state,
        out EmailOutboxWorkerOptions workerOptions)
    {
        var serviceOptions = new EmailServiceOptions
        {
            Provider = "smtp",
            SmtpServer = "smtp.example.com",
            SmtpUserName = "sender@example.com",
            SmtpPassword = "password",
            SmtpPort = 587
        };
        workerOptions = new EmailOutboxWorkerOptions();

        switch (state)
        {
            case EmailCapabilityState.NotConfigured:
                serviceOptions.Provider = "AzureCommunication";
                serviceOptions.AcsConnectionString = "";
                serviceOptions.AcsSenderAddress = "";
                break;

            case EmailCapabilityState.Invalid:
                workerOptions.PollIntervalSeconds = -2;
                break;

            case EmailCapabilityState.Disabled:
                workerOptions.Enabled = false;
                break;
        }

        var evaluator = new EmailCapabilityStatusEvaluator(
            new EmailServiceOptionsValidator(),
            new EmailOutboxWorkerOptionsValidator());
        var status = evaluator.Evaluate(serviceOptions, workerOptions);
        Assert.Equal(state, status.State);
        return status;
    }

    private sealed class TestableEmailOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOutboxWorkerOptions> options,
        EmailCapabilityStatus capabilityStatus,
        ILogger<EmailOutboxWorker> logger)
        : EmailOutboxWorker(scopeFactory, options, capabilityStatus, logger)
    {
        public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
    }

    private sealed class CancellingEmailOutboxMessageProcessor(CancellationTokenSource cancellationSource)
        : IEmailOutboxMessageProcessor
    {
        public int CallCount { get; private set; }

        public Task<bool> ProcessNextAsync(string workerId, CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellationSource.Cancel();
            return Task.FromResult(false);
        }
    }
}
