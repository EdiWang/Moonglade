using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Setup;

public interface IStartUpInitializer
{
    Task<InitStartUpResult> InitStartUp();
}

public class StartUpInitializer(
    ILogger<StartUpInitializer> logger,
    BlogDbContext context,
    IBlogConfigInitializer blogConfigInitializer,
    IMigrationManager migrationManager,
    ISiteIconInitializer siteIconInitializer) : IStartUpInitializer
{
    public async Task<InitStartUpResult> InitStartUp()
    {
        // Create database
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception e)
        {
            logger.LogCritical(e, e.Message);
            return InitStartUpResult.FailedCreateDatabase;
        }

        // Seed database
        bool isNew = !await context.BlogConfiguration.AnyAsync();
        if (isNew)
        {
            try
            {
                logger.LogInformation("Seeding database...");

                await context.ClearAllData();
                await Seed.SeedAsync(context, logger);

                logger.LogInformation("Database seeding successfully.");
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                return InitStartUpResult.FailedSeedingDatabase;
            }
        }

        // Initialize blog configuration
        try
        {
            await blogConfigInitializer.Initialize(isNew);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, e.Message);
            return InitStartUpResult.FailedInitBlogConfig;
        }

        // Database migration for upgrade scenario
        if (!isNew)
        {
            try
            {
                await migrationManager.TryMigration(context);
            }
            catch (Exception e)
            {
                logger.LogCritical(e, e.Message);
                return InitStartUpResult.FailedDatabaseMigration;
            }
        }

        await siteIconInitializer.GenerateSiteIcons();

        return InitStartUpResult.Success;
    }
}

public enum InitStartUpResult
{
    Success = 0,
    FailedCreateDatabase,
    FailedSeedingDatabase,
    FailedInitBlogConfig,
    FailedDatabaseMigration
}