using LiteBus.Commands.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Utils;
using System.Text.RegularExpressions;

namespace Moonglade.Setup;

public interface IMigrationManager
{
    Task TryMigration(BlogDbContext context);
}

public partial class MigrationManager(
    ILogger<MigrationManager> logger,
    ICommandMediator commandMediator,
    IConfiguration configuration,
    IBlogConfig blogConfig,
    IHttpClientFactory httpClientFactory) : IMigrationManager
{
    public async Task TryMigration(BlogDbContext context)
    {
        logger.LogInformation(
            "Found manifest, VersionString: {VersionString}, installed on {InstallTimeUtc} UTC",
            blogConfig.SystemManifestSettings.VersionString,
            blogConfig.SystemManifestSettings.InstallTimeUtc);

        if (!GetAutoMigrationEnabled())
        {
            logger.LogWarning("Automatic database migration is disabled. Enable `Setup:AutoDatabaseMigration` to allow automatic migrations.");
            return;
        }

        if (Helper.IsNonStableVersion())
        {
            logger.LogWarning("Database migration is not supported on non-stable version. Skipped.");
            return;
        }

        var manifestVersion = Version.Parse(blogConfig.SystemManifestSettings.VersionString);
        var currentVersion = Version.Parse(Helper.AppVersionBasic);

        // Only migrate if major or minor version changed
        if (manifestVersion >= currentVersion ||
            (manifestVersion.Major == currentVersion.Major && manifestVersion.Minor == currentVersion.Minor))
        {
            logger.LogInformation("No database migration required.");
            return;
        }

        var provider = context.Database.ProviderName;
        var migrationScriptUrl = GetMigrationScriptUrl(provider);

        if (string.IsNullOrWhiteSpace(migrationScriptUrl))
        {
            var message = $"Automatic database migration is not supported for provider `{provider}`. Please migrate manually.";
            logger.LogCritical(message);
            throw new NotSupportedException(message);
        }

        logger.LogInformation("Migrating database from {FromVersion} to {ToVersion} using provider {Provider}.",
            manifestVersion, currentVersion, provider);

        migrationScriptUrl += $"?nonce={Guid.NewGuid()}";

        try
        {
            await ExecuteMigrationScriptAsync(context, migrationScriptUrl);

            blogConfig.SystemManifestSettings.VersionString = Helper.AppVersionBasic;
            blogConfig.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
            var kvp = blogConfig.UpdateAsync(blogConfig.SystemManifestSettings);

            await commandMediator.SendAsync(new UpdateConfigurationCommand(kvp.Key, kvp.Value));
            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            throw;
        }
    }

    private bool GetAutoMigrationEnabled()
        => bool.TryParse(configuration["Setup:AutoDatabaseMigration"], out var enabled) && enabled;

    private string GetMigrationScriptUrl(string provider)
    {
        return provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => configuration["Setup:DatabaseMigrationScript:SqlServer"],
            "Pomelo.EntityFrameworkCore.MySql" => configuration["Setup:DatabaseMigrationScript:MySql"],
            "Npgsql.EntityFrameworkCore.PostgreSQL" => configuration["Setup:DatabaseMigrationScript:PostgreSql"],
            _ => null
        };
    }

    private async Task ExecuteMigrationScriptAsync(DbContext context, string scriptUrl)
    {
        if (scriptUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Local file system migration script is not supported.");

        if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("Invalid migration script URL.");

        var client = httpClientFactory.CreateClient("MigrationManager");
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"Moonglade/{Helper.AppVersionBasic}");

        logger.LogInformation("Downloading migration script from {Url}...", scriptUrl);

        var response = await client.GetAsync(scriptUrl);
        response.EnsureSuccessStatusCode();

        var script = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(script))
        {
            logger.LogInformation("Executing migration script...");
            await ExecuteMigrationScriptBatchesAsync(script, context);
            logger.LogInformation("Migration script executed successfully.");
        }
        else
        {
            logger.LogWarning("Migration script fetched is empty.");
        }
    }

    private async Task ExecuteMigrationScriptBatchesAsync(string script, DbContext context)
    {
        var batches = SqlBatchSplitterRegex().Split(script)
            .Select(batch => batch.Trim())
            .Where(batch => !string.IsNullOrWhiteSpace(batch))
            .ToArray();

        logger.LogInformation("Split migration script into {Count} batches.", batches.Length);

        for (int i = 0; i < batches.Length; i++)
        {
            logger.LogInformation("Executing batch {Index} of {Total}...", i + 1, batches.Length);
            await context.Database.ExecuteSqlRawAsync(batches[i]);
        }
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline, "en-US")]
    private static partial Regex SqlBatchSplitterRegex();
}
