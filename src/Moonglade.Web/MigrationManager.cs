using Microsoft.EntityFrameworkCore;

namespace Moonglade.Web;

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

        if (!bool.Parse(configuration["Experimental:AutoMSSQLDatabaseMigration"]!))
        {
            logger.LogWarning("Automatic database migration is disabled, if you need, please enable the flag in `Experimental:AutoMSSQLDatabaseMigration`.");
        }

        var mfv = Version.Parse(blogConfig.SystemManifestSettings.VersionString);
        var cuv = Version.Parse(Helper.AppVersionBasic);

        if (mfv < cuv)
        {
            // do not migrate revision
            if (mfv.Major == cuv.Major && mfv.Minor == cuv.Minor)
            {
                logger.LogInformation("No database migration required.");
                return;
            }

            logger.LogInformation("Starting database migration...");

            var dbProvider = context.Database.ProviderName;
            if (dbProvider != "Microsoft.EntityFrameworkCore.SqlServer")
            {
                var message = $"Automatic database migration is not supported on `{dbProvider}` at this time, please migrate your database manually.";
                logger.LogCritical(message);
                throw new NotSupportedException(message);
            }

            logger.LogInformation($"Migrating from {mfv.Major}.{mfv.Minor} to {cuv.Major}.{cuv.Minor}...");

            string mssqlMigrationScriptUrl = $"https://raw.githubusercontent.com/EdiWang/Moonglade/master/Deployment/mssql-migration.sql?nonce={Guid.NewGuid()}";
            await ExecuteMigrationScript(context, mssqlMigrationScriptUrl);

            blogConfig.SystemManifestSettings.VersionString = Helper.AppVersionBasic;
            blogConfig.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
            var kvp = blogConfig.UpdateAsync(blogConfig.SystemManifestSettings);

            await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));

            logger.LogInformation("Database migration completed.");
        }
    }

    private static async Task ExecuteMigrationScript(DbContext context, string scriptUrl)
    {
        // Validate the scriptUrl must not come from local file system
        if (scriptUrl.StartsWith("file://"))
        {
            throw new NotSupportedException("Local file system migration script is not supported.");
        }

        using var client = new HttpClient();

        // Set HTTP headers. Set user-agent as `Moonglade/version`
        client.DefaultRequestHeaders.Add("User-Agent", $"Moonglade/{Helper.AppVersionBasic}");

        var response = await client.GetAsync(scriptUrl);
        response.EnsureSuccessStatusCode();

        var script = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(script))
        {
            // Execute migration script
            await context.Database.ExecuteSqlRawAsync(script);
        }
    }
}