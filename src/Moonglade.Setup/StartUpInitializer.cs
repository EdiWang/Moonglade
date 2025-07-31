using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Setup;

public interface IStartUpInitializer
{
    Task<InitStartUpResult> InitStartUp(CancellationToken cancellationToken = default);
}

public class StartUpInitializer(
    ILogger<StartUpInitializer> logger,
    BlogDbContext context,
    IBlogConfigInitializer blogConfigInitializer,
    IMigrationManager migrationManager,
    ISiteIconInitializer siteIconInitializer) : IStartUpInitializer
{
    public async Task<InitStartUpResult> InitStartUp(CancellationToken cancellationToken = default)
    {
        // Step 1: Ensure database is created
        if (!await TryAsync(
                () => context.Database.EnsureCreatedAsync(cancellationToken),
                "Failed to create database."))
        {
            return InitStartUpResult.FailedCreateDatabase;
        }

        // Step 2: Seed database if new
        bool isNew = !await context.BlogConfiguration.AnyAsync(cancellationToken);
        if (isNew)
        {
            if (!await TryAsync(
                    async () =>
                    {
                        logger.LogInformation("Seeding database...");
                        await context.ClearAllData();
                        await Seed.SeedAsync(context, logger);
                        logger.LogInformation("Database seeded successfully.");
                    },
                    "Failed to seed database."))
            {
                return InitStartUpResult.FailedSeedingDatabase;
            }
        }

        // Step 3: Initialize blog configuration
        if (!await TryAsync(
                () => blogConfigInitializer.Initialize(isNew),
                "Failed to initialize blog configuration."))
        {
            return InitStartUpResult.FailedInitBlogConfig;
        }

        // Step 4: Migrate database if not new
        if (!isNew)
        {
            if (!await TryAsync(
                    async () =>
                    {
                        var result = await migrationManager.TryMigrationAsync(context);
                        if (!result.Success)
                        {
                            logger.LogError(result.ErrorMessage);
                        }
                    },
                    "Failed to migrate database."))
            {
                return InitStartUpResult.FailedDatabaseMigration;
            }
        }

        // Step 5: Generate site icons (failures here do not block startup)
        await siteIconInitializer.GenerateSiteIcons();

        return InitStartUpResult.Success;
    }

    /// <summary>
    /// Helper to run an async action with error logging and result.
    /// </summary>
    private async Task<bool> TryAsync(
        Func<Task> action,
        string errorMessage)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, errorMessage);
            return false;
        }
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
