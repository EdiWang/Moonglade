using Moonglade.Auth;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moq;
using LiteBus.Messaging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Tests.TestSupport;

internal sealed class ConfigurationCommandRecorder : ICommandMediator, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private IBlogConfig _config = null!;

    public List<ICommand> Commands { get; } = [];
    public BlogDbContext Db { get; }
    public IBlogConfig Config => _config;
    public LocalAccountState Accounts { get; } = new();
    public ConfigurationWriteGate Gate { get; } = new();
    public Exception? ActivityLogFailure { get; set; }
    public IEventMediator EventMediator { get; } = Mock.Of<IEventMediator>();

    public ConfigurationCommandRecorder()
    {
        _connection.Open();
        Db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public void Initialize(IBlogConfig config, LocalAccountSettings? account = null)
    {
        _config = config;
        account ??= LocalAccountSettings.DefaultValue;
        Accounts.Publish(account);
        Db.BlogConfiguration.Add(new BlogConfigurationEntity
        {
            CfgKey = nameof(LocalAccountSettings),
            CfgValue = account.ToJson(),
            LastModifiedTimeUtc = DateTime.UtcNow
        });
        foreach (var property in typeof(IBlogConfig).GetProperties())
        {
            Db.BlogConfiguration.Add(new BlogConfigurationEntity
            {
                CfgKey = property.Name,
                CfgValue = property.GetValue(config).ToJson(),
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
        if (command is UpdateConfigurationCommand update)
        {
            await new UpdateConfigurationCommandHandler(Db, _config, EventMediator, Gate, NullLogger<UpdateConfigurationCommandHandler>.Instance)
                .HandleAsync(update, cancellationToken);
        }
        else if (command is UpdateLocalAccountCommand accountUpdate)
        {
            await new UpdateLocalAccountCommandHandler(Db, Accounts, Gate, EventMediator, NullLogger<UpdateLocalAccountCommandHandler>.Instance)
                .HandleAsync(accountUpdate, cancellationToken);
        }
        else if (command is CreateActivityLogCommand && ActivityLogFailure is not null)
        {
            throw ActivityLogFailure;
        }
    }

    public Task<TCommandResult> SendAsync<TCommandResult>(ICommand<TCommandResult> command,
        CommandMediationSettings? settings, CancellationToken cancellationToken) =>
        Task.FromException<TCommandResult>(new NotSupportedException("No result configured for this command."));

    public TCommand Single<TCommand>() where TCommand : ICommand => Commands.OfType<TCommand>().Single();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
