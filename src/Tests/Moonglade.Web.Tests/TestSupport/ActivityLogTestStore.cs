using Edi.CacheAside.InMemory;
using LiteBus.Commands;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Messaging;
using LiteBus.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.BackgroundServices;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Email;
using Moonglade.Email.Core;
using Moonglade.Features.Comment;
using Moonglade.Theme;
using Moonglade.Web.Handlers;
using Moq;

namespace Moonglade.Web.Tests.TestSupport;

public sealed class ActivityLogTestStore : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    public ServiceProvider Services { get; private set; } = null!;
    public Mock<ILogger<Moonglade.Web.Controllers.ActivityLogController>> ControllerLogger { get; } = new();
    public Mock<ILogger<CreateActivityLogCommandHandler>> WriterLogger { get; } = new();
    public BlogConfig Config { get; } = new(BlogConfig.Parse(new Dictionary<string, string>()).Snapshot);

    public static async Task<ActivityLogTestStore> CreateAsync(int historyCount = 0, CancellationToken ct = default)
    {
        var store = new ActivityLogTestStore();
        await store._connection.OpenAsync(ct);
        var services = new ServiceCollection();
        store.ConfigureServices(services);
        store.Services = services.BuildServiceProvider();
        using var scope = store.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlogDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
        for (var index = 0; index < historyCount; index++)
            db.ActivityLog.Add(new ActivityLogEntity
            {
                EventKey = index % 2 == 0 ? ActivityEvents.PostCreated : ActivityEvents.CommentDeleted,
                EventTimeUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc).AddMinutes(index),
                ActorKind = ActivityActorKind.Administrator,
                ActorDisplayName = "historical-admin",
                TargetId = $"snapshot-{index}",
                TargetDisplayName = $"Historical target {index}",
                Details = "{}",
                IpAddress = "192.0.2.10",
                UserAgent = "historical-agent"
            });
        await db.SaveChangesAsync(ct);
        return store;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddDbContext<BlogDbContext>(options => options.UseSqlite(_connection));
        services.AddSingleton<IBlogConfig>(Config);
        services.AddSingleton(Config);
        services.AddSingleton<ConfigurationWriteGate>();
        services.AddInMemoryCacheAside();
        services.AddSingleton<PostCacheInvalidator>();
        services.AddSingleton<ThemeCacheInvalidator>();
        services.AddSingleton<ScheduledPublishWakeUp>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<AuthenticationSettings>(_ => { });
        services.AddSingleton<ILocalAccountTotpService, LocalAccountTotpService>();
        services.AddScoped<OwnerAccountStore>();
        services.AddScoped<IOwnerProfileReader>(provider => provider.GetRequiredService<OwnerAccountStore>());
        services.AddSingleton(ControllerLogger.Object);
        services.AddSingleton(WriterLogger.Object);
        services.AddScoped<IEmailNotificationQueue, DbEmailNotificationQueue>();
        services.AddSingleton(new EmailCapabilityStatusEvaluator(new EmailServiceOptionsValidator(), new EmailOutboxWorkerOptionsValidator())
            .Evaluate(new EmailServiceOptions
            {
                SmtpServer = "smtp.example.invalid",
                SmtpSenderAddress = "blog@example.com",
                SmtpUserName = "synthetic-user",
                SmtpPassword = "synthetic-test-value"
            }, new EmailOutboxWorkerOptions()));
        services.AddLiteBus(bus =>
        {
            bus.AddMessaging(_ => { });
            bus.AddCommands(module =>
            {
                module.RegisterFromAssembly(typeof(ClearActivityLogsCommand).Assembly);
                module.RegisterFromAssembly(typeof(CreateCommentCommand).Assembly);
                module.RegisterFromAssembly(typeof(UpdateConfigurationCommand).Assembly);
            });
            bus.AddQueries(module =>
            {
                module.RegisterFromAssembly(typeof(ListActivityLogsQuery).Assembly);
                module.RegisterFromAssembly(typeof(GetSiteThemeStyleSheetQuery).Assembly);
            });
            bus.AddEvents(module =>
            {
                module.RegisterFromAssembly(typeof(CommentNotificationEventHandler).Assembly);
                module.RegisterFromAssembly(typeof(ConfigurationChangedEventHandler).Assembly);
            });
        });
    }

    public async Task<List<ActivityLogEntity>> ReadLogsAsync(CancellationToken ct = default)
    {
        using var scope = Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<BlogDbContext>()
            .ActivityLog.AsNoTracking().OrderBy(row => row.Id).ToListAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await Services.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
