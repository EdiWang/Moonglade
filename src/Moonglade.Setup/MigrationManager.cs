using Edi.AspNetCore.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Moonglade.Setup;

public interface IMigrationManager
{
    Task<MigrationResult> TryMigrationAsync(BlogDbContext context, CancellationToken cancellationToken = default);
}

public enum MigrationStatus
{
    Success = 0,
    NotRequired,
    Skipped,
    UnsupportedVersion,
    VersionParsingError,
    UnsupportedProvider,
    Failed
}

public record MigrationResult(MigrationStatus Status, string ErrorMessage = null, Version FromVersion = null, Version ToVersion = null)
{
    public bool CanContinueStartup => Status is MigrationStatus.Success or MigrationStatus.NotRequired or MigrationStatus.Skipped;
}

public partial class MigrationManager(
    ILogger<MigrationManager> logger,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment) : IMigrationManager
{
    public async Task<MigrationResult> TryMigrationAsync(BlogDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!hostEnvironment.IsProduction())
        {
            logger.LogInformation(
                "Automatic database migration is skipped in the {EnvironmentName} environment.",
                hostEnvironment.EnvironmentName);
            return new MigrationResult(MigrationStatus.Skipped);
        }

        if (!GetAutoMigrationEnabled())
        {
            const string message = "Automatic database migration is disabled. Skipping migration; database compatibility is the operator's responsibility.";
            logger.LogWarning(message);
            return new MigrationResult(MigrationStatus.Skipped, message);
        }

        SystemManifestSettings manifest;
        try
        {
            manifest = await LoadManifestAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load SystemManifestSettings from the database.");
            return new MigrationResult(MigrationStatus.VersionParsingError, ex.Message);
        }

        logger.LogInformation(
            "Found manifest, VersionString: {VersionString}, installed on {InstallTimeUtc} UTC",
            manifest.VersionString,
            manifest.InstallTimeUtc);

        if (!TryParseVersions(manifest, out var manifestVersion, out var currentVersion, out var versionError))
        {
            logger.LogError("Version parsing failed: {Error}", versionError);
            return new MigrationResult(MigrationStatus.VersionParsingError, versionError);
        }

        // Only migrate if major or minor version changed
        if (!ShouldMigrate(manifestVersion, currentVersion))
        {
            logger.LogInformation("No database migration required.");
            return new MigrationResult(MigrationStatus.NotRequired, "No migration required", manifestVersion, currentVersion);
        }

        if (VersionHelper.IsNonStableVersion())
        {
            const string message = "Database migration is not supported on a non-stable application version.";
            logger.LogWarning(message);
            return new MigrationResult(MigrationStatus.UnsupportedVersion, message, manifestVersion, currentVersion);
        }

        var provider = context.Database.ProviderName;
        var providerKey = GetProviderKey(provider);

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            var message = $"Automatic database migration is not supported for provider `{provider}`. Please migrate manually.";
            logger.LogCritical("Automatic database migration is not supported for provider '{Provider}'. Please migrate manually.", provider);
            return new MigrationResult(MigrationStatus.UnsupportedProvider, message, manifestVersion, currentVersion);
        }

        logger.LogInformation("Migrating database from {FromVersion} to {ToVersion} using provider {Provider}.",
            manifestVersion, currentVersion, provider);

        try
        {
            await ExecuteMigrationAsync(context, providerKey, cancellationToken);

            logger.LogInformation("Database migration completed successfully.");
            return new MigrationResult(MigrationStatus.Success, null, manifestVersion, currentVersion);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            return new MigrationResult(MigrationStatus.Failed, ex.Message, manifestVersion, currentVersion);
        }
    }

    private static bool TryParseVersions(
        SystemManifestSettings manifest,
        out Version manifestVersion,
        out Version currentVersion,
        out string error)
    {
        manifestVersion = null!;
        currentVersion = null!;

        if (!Version.TryParse(manifest.VersionString, out manifestVersion))
        {
            error = $"Invalid manifest version string: {manifest.VersionString}";
            return false;
        }

        if (!Version.TryParse(VersionHelper.AppVersionBasic, out currentVersion))
        {
            error = $"Invalid current version string: {VersionHelper.AppVersionBasic}";
            return false;
        }

        error = null;
        return true;
    }

    private static bool ShouldMigrate(Version manifestVersion, Version currentVersion)
    {
        return manifestVersion < currentVersion &&
               (manifestVersion.Major != currentVersion.Major || manifestVersion.Minor != currentVersion.Minor);
    }

    private async Task ExecuteMigrationAsync(DbContext context, string providerKey, CancellationToken cancellationToken)
    {
        var script = LoadEmbeddedMigrationScript(providerKey);

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException($"Migration script for {providerKey} not found or is empty.");
        }

        logger.LogInformation("Loaded embedded migration script for {Provider}, size: {Size} bytes",
            providerKey, script.Length);

        logger.LogInformation("Executing migration script...");
        await ExecuteMigrationScriptBatchesAsync(script, context, cancellationToken);
        logger.LogInformation("Migration script executed successfully.");
    }

    internal string LoadEmbeddedMigrationScript(string providerKey)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Moonglade.Setup.MigrationScripts.{providerKey}.migration.sql";

        logger.LogInformation("Loading embedded resource: {ResourceName}", resourceName);

        using var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // List all available resources for debugging
            var availableResources = assembly.GetManifestResourceNames();
            logger.LogError("Available embedded resources: {Resources}",
                string.Join(", ", availableResources));

            throw new InvalidOperationException(
                $"Embedded migration script '{resourceName}' not found. " +
                $"Available resources: {string.Join(", ", availableResources)}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task<SystemManifestSettings> LoadManifestAsync(
        BlogDbContext context,
        CancellationToken cancellationToken)
    {
        var json = await context.BlogConfiguration
            .AsNoTracking()
            .Where(item => item.CfgKey == nameof(SystemManifestSettings))
            .Select(item => item.CfgValue)
            .SingleOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("SystemManifestSettings was not found in the database.");
        }

        return json.FromJson<SystemManifestSettings>()
            ?? throw new InvalidOperationException("SystemManifestSettings could not be deserialized.");
    }

    private bool GetAutoMigrationEnabled()
        => configuration.GetValue<bool>("AutoDatabaseMigration");

    private static string GetProviderKey(string provider)
    {
        return provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => "SqlServer",
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "PostgreSql",
            _ => null
        };
    }

    internal async Task ExecuteMigrationScriptBatchesAsync(string script, DbContext context, CancellationToken cancellationToken)
    {
        var batches = SplitScriptIntoBatches(script);

        logger.LogInformation("Split migration script into {Count} batches.", batches.Length);

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            for (int i = 0; i < batches.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                logger.LogInformation("Executing batch {Index} of {Total}...", i + 1, batches.Length);
                await ExecuteBatchAsync(context, batches[i], cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task ExecuteBatchAsync(
        DbContext context,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string[] SplitScriptIntoBatches(string script)
    {
        return SqlBatchSplitterRegex().Split(script)
            .Select(batch => batch.Trim())
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    private static partial Regex SqlBatchSplitterRegex();
}
