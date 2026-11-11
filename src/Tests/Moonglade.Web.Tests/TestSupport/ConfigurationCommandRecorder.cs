using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moonglade.ActivityLog;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moq;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class ConfigurationCommandRecorder : ICommandMediator, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private BlogConfig _config = null!;

    public List<ICommand> Commands { get; } = [];
    public BlogDbContext Db { get; }
    public IBlogConfig Config => _config;
    public OwnerAccountStore Accounts { get; }
    public ConfigurationWriteGate Gate { get; } = new();
    public Exception? ActivityLogFailure { get; set; }
    public bool PersistActivityLogs { get; set; }
    public Mock<ILogger<CreateActivityLogCommandHandler>> ActivityLogger { get; } = new();
    public IEventMediator EventMediator { get; } = Mock.Of<IEventMediator>();

    public ConfigurationCommandRecorder()
    {
        _connection.Open();
        Db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
        Accounts = new OwnerAccountStore(Db, new LocalAccountTotpService(),
            Options.Create(new AuthenticationSettings { Totp = new TotpAuthenticationSettings { Required = false } }), TimeProvider.System, EventMediator);
    }

    public void Initialize(BlogConfig config)
    {
        _config = config;
        foreach (var property in typeof(BlogSettingsSnapshot).GetProperties())
        {
            Db.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = property.Name,
                CfgValue = property.GetValue(config.Capture()).ToJson(),
                LastModifiedTimeUtc = DateTime.UtcNow
            });
        }
        Db.SaveChanges();
    }

    public Task<MediationResult> TrySendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<MediationResult<TCommandResult>> TrySendAsync<TCommandResult>(ICommand<TCommandResult> command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<MediationDecision> EvaluateAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public async Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
    {
        Commands.Add(command);
        if (command is CreateActivityLogCommand && ActivityLogFailure is not null)
        {
            throw ActivityLogFailure;
        }
        else if (command is CreateActivityLogCommand activity && PersistActivityLogs)
        {
            await new CreateActivityLogCommandHandler(Db, ActivityLogger.Object).HandleAsync(activity, cancellationToken);
        }
    }

    public async Task<TCommandResult> SendAsync<TCommandResult>(ICommand<TCommandResult> command,
        CommandMediationSettings? settings, CancellationToken cancellationToken)
    {
        Commands.Add(command);
        var handler = new UpdateConfigurationCommandHandler(Db, _config, EventMediator, Gate,
            NullLogger<UpdateConfigurationCommandHandler>.Instance);
        if (command is UpdateConfigurationCommand update)
            return (TCommandResult)(object)await handler.HandleAsync(update, cancellationToken);
        if (command is PatchConfigurationCommand patch)
        {
            return (TCommandResult)(object)await handler.HandleAsync(patch, cancellationToken);
        }

        throw new NotSupportedException("No result configured for this command.");
    }

    public TCommand Single<TCommand>() where TCommand : ICommand => Commands.OfType<TCommand>().Single();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
