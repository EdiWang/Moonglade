using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.PostgreSql;
using Moonglade.Data.SqlServer;
using Npgsql;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Moonglade.Setup.Tests.Database;

public sealed class DisposableDatabaseTheoryAttribute : TheoryAttribute
{
    public DisposableDatabaseTheoryAttribute([CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
    {
        if (!DisposableCategoryDatabases.Enabled)
            Skip = "Opt in with MOONGLADE_TEST_CATEGORY_DATABASES=1; Docker must be running. Only uniquely named disposable test containers are used.";
    }
}

public sealed class DisposableCategoryDatabases : IAsyncLifetime
{
    public static bool Enabled => Environment.GetEnvironmentVariable("MOONGLADE_TEST_CATEGORY_DATABASES") == "1";
    private readonly Dictionary<string, string> _connections = [];
    private readonly List<string> _containers = [];

    public async ValueTask InitializeAsync()
    {
        if (!Enabled) return;
        try
        {
            await StartAsync("PostgreSql", "postgres:18-alpine", "5432", "postgres");
            await StartAsync("SqlServer", "mcr.microsoft.com/mssql/server:2022-latest", "1433", "master");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var name in _containers)
            await DockerAsync(["rm", "--force", name]);
        _containers.Clear();
    }

    public async Task<CategoryTestDatabase> CreateAsync(string provider)
    {
        Assert.True(Enabled, "Disposable database tests must be explicitly enabled.");
        var name = "category_tests_" + Guid.NewGuid().ToString("N");
        await using var admin = Connection(provider, _connections[provider]);
        await admin.OpenAsync(TestContext.Current.CancellationToken);
        await using var create = admin.CreateCommand();
        create.CommandText = $"CREATE DATABASE {name}";
        await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        var connectionString = provider == "PostgreSql"
            ? new NpgsqlConnectionStringBuilder(_connections[provider]) { Database = name }.ConnectionString
            : new SqlConnectionStringBuilder(_connections[provider]) { InitialCatalog = name }.ConnectionString;
        var connection = Connection(provider, connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        return new CategoryTestDatabase(provider, connection);
    }

    private async Task StartAsync(string provider, string image, string port, string database)
    {
        var name = $"moonglade-category-tests-{provider.ToLowerInvariant()}-{Guid.NewGuid():N}";
        var password = $"CategoryTests!{Guid.NewGuid():N}";
        string[] environment = provider == "PostgreSql"
            ? ["--env", "POSTGRES_USER=category_tests", "--env", $"POSTGRES_PASSWORD={password}", "--env", "POSTGRES_DB=postgres"]
            : ["--env", "ACCEPT_EULA=Y", "--env", "MSSQL_PID=Developer", "--env", $"MSSQL_SA_PASSWORD={password}"];
        await DockerAsync(["run", "--detach", "--name", name, "--label", "moonglade.category-tests=true",
            "--publish", $"127.0.0.1::{port}", .. environment, image]);
        _containers.Add(name);
        var binding = (await DockerAsync(["port", name, $"{port}/tcp"])).Trim();
        var localPort = int.Parse(binding[(binding.LastIndexOf(':') + 1)..], CultureInfo.InvariantCulture);
        var connectionString = provider == "PostgreSql"
            ? new NpgsqlConnectionStringBuilder
            {
                Host = "127.0.0.1", Port = localPort, Database = database, Username = "category_tests",
                Password = password, Timeout = 2, Pooling = false
            }.ConnectionString
            : new SqlConnectionStringBuilder
            {
                DataSource = $"127.0.0.1,{localPort}", InitialCatalog = database, UserID = "sa",
                Password = password, ConnectTimeout = 2, TrustServerCertificate = true, Pooling = false
            }.ConnectionString;
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            try
            {
                await using var connection = Connection(provider, connectionString);
                await connection.OpenAsync(TestContext.Current.CancellationToken);
                break;
            }
            catch (DbException) when (DateTime.UtcNow < deadline)
            {
                // This delay waits for our database process, not application behavior.
                await Task.Delay(500, TestContext.Current.CancellationToken);
            }
        }
        _connections[provider] = connectionString;
    }

    private static DbConnection Connection(string provider, string connectionString) =>
        provider == "PostgreSql" ? new NpgsqlConnection(connectionString) : new SqlConnection(connectionString);

    private static async Task<string> DockerAsync(string[] arguments)
    {
        var start = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Docker.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await process.WaitForExitAsync(timeout.Token);
        var result = await output;
        var diagnostic = await error;
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Disposable Docker operation failed: {diagnostic}");
        return result;
    }
}

public sealed class CategoryTestDatabase(string provider, DbConnection connection) : IAsyncDisposable
{
    public string Provider { get; } = provider;
    public DbConnection Connection { get; } = connection;
    public BlogDbContext Context() => Provider == "PostgreSql"
        ? new PostgreSqlBlogDbContext(new DbContextOptionsBuilder<PostgreSqlBlogDbContext>().UseNpgsql(Connection).Options)
        : new SqlServerBlogDbContext(new DbContextOptionsBuilder<SqlServerBlogDbContext>().UseSqlServer(Connection).Options);

    public async Task ExecuteAsync(string sql)
    {
        await using var command = Connection.CreateCommand();
        command.CommandTimeout = 60;
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    public async Task<string[][]> RowsAsync(string sql)
    {
        await using var command = Connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<string[]>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            rows.Add(Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "<null>" :
                reader.GetValue(i) is DateTime date ? date.ToString("O", CultureInfo.InvariantCulture) :
                Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture)!).ToArray());
        return rows.ToArray();
    }

    public async Task MigrateAsync()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "database", Provider, "category-int-identity.sql")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        var sql = await File.ReadAllTextAsync(Path.Combine(directory!.FullName, "database", Provider, "category-int-identity.sql"),
            TestContext.Current.CancellationToken);
        try
        {
            await ExecuteAsync(sql);
        }
        catch (DbException)
        {
            if (Provider == "PostgreSql") await ExecuteAsync("ROLLBACK;");
            throw;
        }
    }

    public ValueTask DisposeAsync() => Connection.DisposeAsync();
}
