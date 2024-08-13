using Microsoft.EntityFrameworkCore;
using Moonglade.Data.SqlServer;

namespace Moonglade.Web;

public class MigrationManager
{
    public static async Task TryMigration(WebApplication app)
    {
        var bc = app.Services.GetRequiredService<IBlogConfig>();
        app.Logger.LogInformation($"Found manifest, VersionString: {bc.SystemManifestSettings.VersionString}, installed on {bc.SystemManifestSettings.InstallTimeUtc} UTC");

        if (!bool.Parse(app.Configuration["Experimental:AutoMSSQLDatabaseMigration"]!))
        {
            app.Logger.LogWarning("Automatic database migration is disabled, if you need, please enable the flag in `Experimental:AutoMSSQLDatabaseMigration`.");
        }

        var mfv = Version.Parse(bc.SystemManifestSettings.VersionString);
        var cuv = Version.Parse(Helper.AppVersionBasic);

        if (mfv < cuv)
        {
            // do not migrate revision
            if (mfv.Major == cuv.Major && mfv.Minor == cuv.Minor)
            {
                app.Logger.LogInformation("No database migration required.");
                return;
            }

            app.Logger.LogInformation("Starting database migration...");

            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            string dbType = app.Configuration.GetConnectionString("DatabaseType")!;
            if (dbType.ToLower() != "sqlserver")
            {
                var message = $"Automatic database migration is not supported on `{dbType}` at this time, please migrate your database manually.";
                app.Logger.LogCritical(message);
                throw new NotSupportedException(message);
            }

            var context = services.GetRequiredService<SqlServerBlogDbContext>();

            app.Logger.LogInformation($"Migrating from {mfv.Major}.{mfv.Minor} to {cuv.Major}.{cuv.Minor}...");

            string mssqlMigrationScriptUrl = $"https://raw.githubusercontent.com/EdiWang/Moonglade/master/Deployment/mssql-migration.sql";
            await ExecuteMigrationScript(context, mssqlMigrationScriptUrl);

            bc.SystemManifestSettings.VersionString = Helper.AppVersionBasic;
            bc.SystemManifestSettings.InstallTimeUtc = DateTime.UtcNow;
            var kvp = bc.UpdateAsync(bc.SystemManifestSettings);

            var mediator = services.GetRequiredService<IMediator>();
            await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value));

            app.Logger.LogInformation("Database migration completed.");
        }
    }

    private static async Task ExecuteMigrationScript(BlogDbContext context, string scriptUrl)
    {
        // Validate the scriptUrl must not come from local file system
        if (scriptUrl.StartsWith("file://"))
        {
            throw new NotSupportedException("Local file system migration script is not supported.");
        }

        using var client = new HttpClient();
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