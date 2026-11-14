using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Features.Asset;
using Moq;

namespace Moonglade.Setup.Tests.TestSupport;

internal sealed class StartupTestHarness : IDisposable
{
    private readonly BlogDbContext _db;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "moonglade-startup-tests", Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _tempVariables = new[] { "TEMP", "TMP", "TMPDIR" }
        .ToDictionary(name => name, Environment.GetEnvironmentVariable);

    public BlogConfig Config { get; } = new();
    public SystemManifestSettings Manifest => SystemManifestSettings.ParseStored(
        _db.BlogConfiguration.AsNoTracking().Single(row => row.CfgKey == nameof(SystemManifestSettings)).CfgValue);
    public RecordingLogger<ConfigInitializer> ConfigLogger { get; } = new();
    public StartUpInitializer Initializer { get; }

    public StartupTestHarness(BlogDbContext db)
    {
        _db = db;
        Directory.CreateDirectory(_directory);
        foreach (var name in _tempVariables.Keys)
            Environment.SetEnvironmentVariable(name, _directory);

        var queries = new Mock<IQueryMediator>();
        var listHandler = new ListConfigurationsQueryHandler(db);
        queries.Setup(m => m.QueryAsync(It.IsAny<ListConfigurationsQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((IQuery<IDictionary<string, string>> command, QueryMediationSettings _, CancellationToken ct) =>
                listHandler.HandleAsync((ListConfigurationsQuery)command, ct));
        queries.Setup(m => m.QueryAsync(It.IsAny<GetAssetQuery>(), It.IsAny<QueryMediationSettings>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Test icon source is unavailable."));
        var commands = new Mock<ICommandMediator>();
        var defaultHandler = new AddDefaultConfigurationCommandHandler(db, NullLogger<AddDefaultConfigurationCommandHandler>.Instance);
        commands.Setup(m => m.SendAsync(It.IsAny<AddDefaultConfigurationCommand>(), It.IsAny<CommandMediationSettings>(), It.IsAny<CancellationToken>()))
            .Returns((ICommand<OperationCode> command, CommandMediationSettings _, CancellationToken ct) =>
                defaultHandler.HandleAsync((AddDefaultConfigurationCommand)command, ct));
        var initializer = new ConfigInitializer(queries.Object, commands.Object, Config, ConfigLogger);
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(_directory);
        var icons = new SiteIconBuilder(NullLogger<SiteIconBuilder>.Instance, queries.Object, environment.Object);
        Initializer = new StartUpInitializer(NullLogger<StartUpInitializer>.Instance, db, initializer, icons);
    }

    public void Dispose()
    {
        foreach (var (name, value) in _tempVariables)
            Environment.SetEnvironmentVariable(name, value);
        Directory.Delete(_directory, recursive: true);
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
}
