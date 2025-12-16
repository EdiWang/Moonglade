using Edi.AspNetCore.Utils;
using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
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
    Failed
}

public record MigrationResult(MigrationStatus Status, string ErrorMessage = null, Version FromVersion = null, Version ToVersion = null)
{
    public bool IsSuccess => Status == MigrationStatus.Success || Status == MigrationStatus.NotRequired;
    public bool IsFailed => Status == MigrationStatus.Failed || Status == MigrationStatus.VersionParsingError;
}

public partial class MigrationManager(
    ILogger<MigrationManager> logger,
    ICommandMediator commandMediator,
    IConfiguration configuration,
    IBlogConfig blogConfig,
    IHttpClientFactory httpClientFactory) : IMigrationManager
{
    private const int DefaultHttpTimeoutSeconds = 30;
    private const int MaxScriptSizeBytes = 50 * 1024 * 1024; // 50MB limit

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
        var migrationScriptUrl = GetMigrationScriptUrl(provider);

        if (string.IsNullOrWhiteSpace(migrationScriptUrl))
        {
            var message = $"Automatic database migration is not supported for provider `{provider}`. Please migrate manually.";
            logger.LogCritical(message);
            return new MigrationResult(MigrationStatus.UnsupportedProvider, message, manifestVersion, currentVersion);
        }

        logger.LogInformation("Migrating database from {FromVersion} to {ToVersion} using provider {Provider}.",
            manifestVersion, currentVersion, provider);

        try
        {
            await ExecuteMigrationAsync(context, migrationScriptUrl, cancellationToken);
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

    private async Task ExecuteMigrationAsync(DbContext context, string scriptUrl, CancellationToken cancellationToken)
    {
        var scriptUrlWithNonce = $"{scriptUrl}?nonce={Guid.NewGuid():N}";

        ValidateScriptUrl(scriptUrlWithNonce);

        using var client = CreateHttpClient();

        logger.LogInformation("Downloading migration script from {Url}...", scriptUrl);

        using var response = await client.GetAsync(scriptUrlWithNonce, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Check content length to prevent excessive memory usage
        if (response.Content.Headers.ContentLength > MaxScriptSizeBytes)
        {
            throw new InvalidOperationException($"Migration script is too large: {response.Content.Headers.ContentLength} bytes");
        }

        var script = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(script))
        {
            logger.LogWarning("Migration script fetched is empty.");
            return;
        }

        logger.LogInformation("Executing migration script...");
        await ExecuteMigrationScriptBatchesAsync(script, context, cancellationToken);
        logger.LogInformation("Migration script executed successfully.");
    }

    private HttpClient CreateHttpClient()
    {
        var client = httpClientFactory.CreateClient("MigrationManager");

        var timeoutSeconds = configuration.GetValue("Setup:MigrationTimeoutSeconds", DefaultHttpTimeoutSeconds);
        client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Moonglade/{VersionHelper.AppVersionBasic}");

        return client;
    }

    private async Task UpdateManifestAsync(CancellationToken cancellationToken)
    {
        blogConfig.SystemManifestSettings.VersionString = VersionHelper.AppVersionBasic;
        blogConfig.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
        var kvp = blogConfig.UpdateAsync(blogConfig.SystemManifestSettings);

        await commandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value), cancellationToken);
    }

    private static void ValidateScriptUrl(string scriptUrl)
    {
        if (scriptUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Local file system migration script is not supported.");

        if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Invalid migration script URL.", nameof(scriptUrl));

        // Additional security check for localhost/private IPs could be added here
        if (IsPrivateOrLocalhost(uriResult.Host))
        {
            throw new SecurityException("Migration script URL cannot point to localhost or private network addresses.");
        }
    }

    private static bool IsPrivateOrLocalhost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(host, "127.0.0.1", StringComparison.Ordinal) ||
            string.Equals(host, "::1", StringComparison.Ordinal))
        {
            return true;
        }

        // Check for private IP ranges (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16)
        if (System.Net.IPAddress.TryParse(host, out var ipAddress))
        {
            var bytes = ipAddress.GetAddressBytes();
            if (bytes.Length == 4) // IPv4
            {
                return bytes[0] == 10 ||
                       (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                       (bytes[0] == 192 && bytes[1] == 168);
            }
        }

        return false;
    }

    private bool GetAutoMigrationEnabled()
        => configuration.GetValue<bool>("Setup:AutoDatabaseMigration");

    private string GetMigrationScriptUrl(string provider)
    {
        return provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => "https://raw.githubusercontent.com/EdiWang/Moonglade/release/Deployment/mssql-migration.sql",
            "Pomelo.EntityFrameworkCore.MySql" => string.Empty,
            "Npgsql.EntityFrameworkCore.PostgreSQL" => string.Empty,
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

// Add this exception class for security-related issues
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}
