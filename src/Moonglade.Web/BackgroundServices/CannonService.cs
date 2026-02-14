using System.Threading.Channels;

namespace Moonglade.Web.BackgroundServices;

/// <summary>
/// A reliable fire-and-forget service backed by <see cref="Channel{T}"/> and <see cref="BackgroundService"/>.
/// Work items are queued in-memory and processed sequentially on a background thread
/// that participates in the application's graceful shutdown lifecycle.
/// </summary>
public class CannonService : BackgroundService
{
    private readonly ILogger<CannonService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<Func<IServiceProvider, Task>> _queue;

    public CannonService(
        ILogger<CannonService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _queue = Channel.CreateUnbounded<Func<IServiceProvider, Task>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });
    }

    /// <summary>
    /// Enqueue a fire-and-forget work item. The dependency <typeparamref name="T"/> is resolved
    /// from a new DI scope when the item is dequeued and executed.
    /// </summary>
    public void FireAsync<T>(Func<T, Task> bullet, Action<Exception> handler = null)
    {
        if (!_queue.Writer.TryWrite(async sp =>
            {
                var dependency = sp.GetRequiredService<T>();
                try
                {
                    await bullet(dependency);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error executing background work item of type {DependencyType}.", typeof(T).Name);
                    handler?.Invoke(e);
                }
            }))
        {
            _logger.LogWarning("Failed to enqueue background work item of type {DependencyType}.", typeof(T).Name);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CannonService background queue is starting.");

        await foreach (var workItem in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                await workItem(scope.ServiceProvider);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled error processing background work item.");
            }
        }

        _logger.LogInformation("CannonService background queue is stopping.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CannonService is stopping. Draining remaining work items...");

        _queue.Writer.Complete();

        await base.StopAsync(cancellationToken);
    }
}