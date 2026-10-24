using LiteBus.Commands.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Tests;

internal sealed class ConfigurationCommandRecorder : ICommandMediator, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private IBlogConfig _config = null!;

    public List<ICommand> Commands { get; } = [];
    public BlogDbContext Db { get; }
    public IBlogConfig Config => _config;

    public ConfigurationCommandRecorder()
    {
        _connection.Open();
        Db = new BlogDbContext(new DbContextOptionsBuilder<BlogDbContext>().UseSqlite(_connection).Options);
        Db.Database.EnsureCreated();
    }

    public void Initialize(IBlogConfig config)
    {
        _config = config;
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

    public async Task SendAsync(ICommand command, CommandMediationSettings? settings, CancellationToken cancellationToken)
    {
        Commands.Add(command);
        if (command is UpdateConfigurationCommand update)
        {
            await new UpdateConfigurationCommandHandler(Db, _config, NullLogger<UpdateConfigurationCommandHandler>.Instance)
                .HandleAsync(update, cancellationToken);
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
