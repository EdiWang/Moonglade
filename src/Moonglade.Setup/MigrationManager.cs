using Edi.AspNetCore.Utils;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    Disabled,
    UnsupportedVersion,
    VersionParsingError,
    UnsupportedProvider,
    Failed,
    ScriptNotFound
}

public record MigrationResult(MigrationStatus Status, string ErrorMessage = null, Version FromVersion = null, Version ToVersion = null)
{
    public bool IsSuccess => Status == MigrationStatus.Success || Status == MigrationStatus.NotRequired;
    public bool IsFailed => Status == MigrationStatus.Failed || Status == MigrationStatus.VersionParsingError || Status == MigrationStatus.ScriptNotFound;
}

public partial class MigrationManager(
    ILogger<MigrationManager> logger,
    ICommandMediator commandMediator,
    IConfiguration configuration,
    IBlogConfig blogConfig) : IMigrationManager
{
    public async Task<MigrationResult> TryMigrationAsync(BlogDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        logger.LogInformation(
            "Found manifest, VersionString: {VersionString}, installed on {InstallTimeUtc} UTC",
            blogConfig.SystemManifestSettings.VersionString,
            blogConfig.SystemManifestSettings.InstallTimeUtc);

        if (!GetAutoMigrationEnabled())
        {
            const string message = "Automatic database migration is disabled. Enable `Setup:AutoDatabaseMigration` to allow automatic migrations.";
            logger.LogWarning(message);
            return new MigrationResult(MigrationStatus.Disabled, message);
        }

        if (VersionHelper.IsNonStableVersion())
        {
            const string message = "Database migration is not supported on non-stable version. Skipped.";
            logger.LogWarning(message);
            return new MigrationResult(MigrationStatus.UnsupportedVersion, message);
        }

        if (!TryParseVersions(out var manifestVersion, out var currentVersion, out var versionError))
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

        var provider = context.Database.ProviderName;
        var providerKey = GetProviderKey(provider);

        if (string.IsNullOrWhiteSpace(providerKey))
        {
            var message = $"Automatic database migration is not supported for provider `{provider}`. Please migrate manually.";
            logger.LogCritical(message);
            return new MigrationResult(MigrationStatus.UnsupportedProvider, message, manifestVersion, currentVersion);
        }

        logger.LogInformation("Migrating database from {FromVersion} to {ToVersion} using provider {Provider}.",
            manifestVersion, currentVersion, provider);

        try
        {
            await ExecuteMigrationAsync(context, providerKey, cancellationToken);
            await UpdateManifestAsync(cancellationToken);

            logger.LogInformation("Database migration completed successfully.");
            return new MigrationResult(MigrationStatus.Success, null, manifestVersion, currentVersion);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            return new MigrationResult(MigrationStatus.Failed, ex.Message, manifestVersion, currentVersion);
        }
    }

    private bool TryParseVersions(out Version manifestVersion, out Version currentVersion, out string error)
    {
        manifestVersion = null!;
        currentVersion = null!;

        if (!Version.TryParse(blogConfig.SystemManifestSettings.VersionString, out manifestVersion))
        {
            error = $"Invalid manifest version string: {blogConfig.SystemManifestSettings.VersionString}";
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

    private string LoadEmbeddedMigrationScript(string providerKey)
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

    private async Task UpdateManifestAsync(CancellationToken cancellationToken)
    {
        blogConfig.SystemManifestSettings.VersionString = VersionHelper.AppVersionBasic;
        blogConfig.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
        var kvp = blogConfig.UpdateAsync(blogConfig.SystemManifestSettings);

        await commandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value), cancellationToken);
    }

    private bool GetAutoMigrationEnabled()
        => configuration.GetValue<bool>("Setup:AutoDatabaseMigration");

    private static string GetProviderKey(string provider)
    {
        return provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => "SqlServer",
            "Pomelo.EntityFrameworkCore.MySql" => "MySql",
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "PostgreSql",
            _ => null
        };
    }

    private async Task ExecuteMigrationScriptBatchesAsync(string script, DbContext context, CancellationToken cancellationToken)
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
                await context.Database.ExecuteSqlRawAsync(batches[i], cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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

// Exception class for security-related issues such as script integrity validation failures
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}
