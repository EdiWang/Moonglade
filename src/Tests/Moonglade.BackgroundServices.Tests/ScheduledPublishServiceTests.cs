using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Features.Post;
using Moq;

namespace Moonglade.BackgroundServices.Tests;

public partial class ScheduledPublishServiceTests
{
    [Fact]
    public async Task DisableWhileWaiting_SkipsPublicationAndRemainsRunning()
    {
        var config = new BlogConfig();
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<GetNextScheduledPostTimeQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddHours(1));
        var commands = new Mock<ICommandMediator>();
        var logger = new SchedulerLogger();
        var wakeUp = new ScheduledPublishWakeUp();
        using var services = Services(queries.Object, commands.Object);
        using var worker = new ScheduledPublishService(services, logger, wakeUp, config);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await logger.Waiting.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            config.Publish(config.Capture() with { AdvancedSettings = new AdvancedSettings { EnablePostScheduler = false } });
            wakeUp.WakeUp();
            await logger.Disabled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Empty(commands.Invocations);
            Assert.False(worker.ExecuteTask!.IsCompleted);
        }
        finally
        {
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task EnableWhileDisabled_WakesAndPublishesDueWorkWithoutFiveMinuteDelay()
    {
        var config = new BlogConfig(new BlogSettingsSnapshot { AdvancedSettings = new AdvancedSettings { EnablePostScheduler = false } });
        var queries = new Mock<IQueryMediator>();
        var calls = 0;
        queries.Setup(m => m.QueryAsync(It.IsAny<GetNextScheduledPostTimeQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Interlocked.Increment(ref calls) == 1 ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddHours(1));
        var published = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<PublishScheduledPostCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                published.TrySetResult();
                return Task.FromResult(1);
            });
        var logger = new SchedulerLogger();
        var wakeUp = new ScheduledPublishWakeUp();
        using var services = Services(queries.Object, commands.Object);
        using var worker = new ScheduledPublishService(services, logger, wakeUp, config);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await logger.Disabled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            config.Publish(config.Capture() with { AdvancedSettings = new AdvancedSettings { EnablePostScheduler = true } });
            wakeUp.WakeUp();
            await published.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(worker.ExecuteTask!.IsCompleted);
        }
        finally
        {
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static ServiceProvider Services(IQueryMediator queries, ICommandMediator commands) => new ServiceCollection()
        .AddSingleton(queries).AddSingleton(commands).BuildServiceProvider();

    [Fact]
    public async Task DisableAfterDispatch_DoesNotCancelPublicationAlreadyRunning()
    {
        var config = new BlogConfig();
        var queries = new Mock<IQueryMediator>();
        queries.Setup(m => m.QueryAsync(It.IsAny<GetNextScheduledPostTimeQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.UtcNow.AddMinutes(-1));
        var dispatched = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new Mock<ICommandMediator>();
        commands.Setup(m => m.SendAsync(It.IsAny<PublishScheduledPostCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((PublishScheduledPostCommand _, CommandMediationSettings _, CancellationToken ct) =>
            {
                dispatched.TrySetResult(ct);
                return finish.Task;
            });
        var logger = new SchedulerLogger();
        var wakeUp = new ScheduledPublishWakeUp();
        using var services = Services(queries.Object, commands.Object);
        using var worker = new ScheduledPublishService(services, logger, wakeUp, config);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            var token = await dispatched.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            config.Publish(config.Capture() with { AdvancedSettings = new AdvancedSettings { EnablePostScheduler = false } });
            wakeUp.WakeUp();
            Assert.False(token.IsCancellationRequested);
            finish.SetResult(1);
            await logger.Disabled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        }
        finally
        {
            finish.TrySetResult(0);
            await worker.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private sealed class SchedulerLogger : ILogger<ScheduledPublishService>
    {
        public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Disabled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<(int? Count, Exception? Failure)> Outcome { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            if (message.StartsWith("Next scheduled publish in", StringComparison.Ordinal)) Waiting.TrySetResult();
            if (message.StartsWith("Scheduled publishing is disabled", StringComparison.Ordinal)) Disabled.TrySetResult();
            if (message.StartsWith("Published ", StringComparison.Ordinal))
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> fields)
                    Outcome.TrySetResult(((int)fields.Single(field => field.Key == "Count").Value!, null));
            }
            if (message.StartsWith("Error in ScheduledPublishService.", StringComparison.Ordinal))
                Outcome.TrySetResult((null, exception));
        }
    }
}
