using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Utils;

namespace Moonglade.Setup;

public interface IMigrationManager
{
    Task TryMigration(BlogDbContext context);
}

public class MigrationManager(
    ILogger<MigrationManager> logger,
    IMediator mediator,
    IConfiguration configuration,
    IBlogConfig blogConfig) : IMigrationManager
{
    public async Task TryMigration(BlogDbContext context)
    {
        logger.LogInformation($"Found manifest, VersionString: {blogConfig.SystemManifestSettings.VersionString}, installed on {blogConfig.SystemManifestSettings.InstallTimeUtc} UTC");

        if (!bool.Parse(configuration["Setup:AutoDatabaseMigration"]!))
        {
            logger.LogWarning("Automatic database migration is disabled, if you need, please enable the flag in `Setup:AutoDatabaseMigration`.");
            return;
        }

        var mfv = Version.Parse(blogConfig.SystemManifestSettings.VersionString);
        var cuv = Version.Parse(Helper.AppVersionBasic);

        if (Helper.IsNonStableVersion())
        {
            logger.LogWarning("Database migration is not supported on non-stable version. Skipped.");
            return;
        }

        if (mfv < cuv)
        {
            // do not migrate revision
            if (mfv.Major == cuv.Major && mfv.Minor == cuv.Minor)
            {
                logger.LogInformation("No database migration required.");
                return;
            }

            logger.LogInformation("Starting database migration...");

            string sqlMigrationScriptUrl = string.Empty;
            var dbProvider = context.Database.ProviderName;
            switch (dbProvider)
            {
                case "Microsoft.EntityFrameworkCore.SqlServer":
                    logger.LogInformation("Database provider: Microsoft SQL Server");
                    sqlMigrationScriptUrl = configuration["Setup:DatabaseMigrationScript:SqlServer"];

                    break;
                case "Pomelo.EntityFrameworkCore.MySql":
                    logger.LogInformation("Database provider: MySQL");
                    sqlMigrationScriptUrl = configuration["Setup:DatabaseMigrationScript:MySql"];

                    break;
                case "Npgsql.EntityFrameworkCore.PostgreSQL":
                    logger.LogInformation("Database provider: PostgreSQL");
                    sqlMigrationScriptUrl = configuration["Setup:DatabaseMigrationScript:PostgreSql"];

                    break;
            }

            if (string.IsNullOrWhiteSpace(sqlMigrationScriptUrl))
            {
                var message = $"Automatic database migration is not supported on `{dbProvider}` at this time, please migrate your database manually.";
                logger.LogCritical(message);
                throw new NotSupportedException(message);
            }

            logger.LogInformation($"Migrating from {mfv.Major}.{mfv.Minor} to {cuv.Major}.{cuv.Minor}...");

            sqlMigrationScriptUrl += $"?nonce={Guid.NewGuid()}";
            await ExecuteMigrationScript(context, sqlMigrationScriptUrl);

            blogConfig.SystemManifestSettings.VersionString = Helper.AppVersionBasic;
            blogConfig.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
            var kvp = blogConfig.UpdateAsync(blogConfig.SystemManifestSettings);

            await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));

            logger.LogInformation("Database migration completed.");
        }
    }

    private async Task ExecuteMigrationScript(DbContext context, string scriptUrl)
    {
        // Validate the scriptUrl must not come from local file system
        if (scriptUrl.StartsWith("file://"))
        {
            throw new NotSupportedException("Local file system migration script is not supported.");
        }

        // Validate the scriptUrl is a valid URL
        if (!Uri.TryCreate(scriptUrl, UriKind.Absolute, out var uriResult) ||
            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("Invalid script URL.");
        }

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        // Set HTTP headers. Set user-agent as `Moonglade/version`
        client.DefaultRequestHeaders.Add("User-Agent", $"Moonglade/{Helper.AppVersionBasic}");

        var response = await client.GetAsync(scriptUrl);
        response.EnsureSuccessStatusCode();

        var script = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(script))
        {
            // Execute migration script
            logger.LogInformation("Executing migration script...");

            await context.Database.ExecuteSqlRawAsync(script);

            logger.LogInformation("Migration script executed successfully.");
        }
    }
}