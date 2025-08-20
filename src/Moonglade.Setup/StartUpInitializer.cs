using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Setup;

public interface IStartUpInitializer
{
    Task<InitStartUpResult> InitStartUpAsync(CancellationToken cancellationToken = default);
}

public class StartUpInitializer(
    ILogger<StartUpInitializer> logger,
    BlogDbContext context,
    IBlogConfigInitializer blogConfigInitializer,
    IMigrationManager migrationManager,
    ISiteIconInitializer siteIconInitializer) : IStartUpInitializer
{
    public async Task<InitStartUpResult> InitStartUpAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting application initialization...");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Step 1: Ensure database is created
            var result = await EnsureDatabaseCreatedAsync(cancellationToken);
            if (result != InitStartUpResult.Success)
                return result;

            // Step 2: Check if database is new and seed if necessary
            var isNewDatabase = await IsDatabaseNewAsync(cancellationToken);
            if (isNewDatabase)
            {
                result = await SeedDatabaseAsync(cancellationToken);
                if (result != InitStartUpResult.Success)
                    return result;
            }

            // Step 3: Initialize blog configuration
            result = await InitializeBlogConfigurationAsync(isNewDatabase, cancellationToken);
            if (result != InitStartUpResult.Success)
                return result;

            // Step 4: Migrate database if not new
            if (!isNewDatabase)
            {
                result = await MigrateDatabaseAsync(cancellationToken);
                if (result != InitStartUpResult.Success)
                    return result;
            }

            // Step 5: Generate site icons (non-blocking operation)
            await GenerateSiteIconsAsync(cancellationToken);

            stopwatch.Stop();
            logger.LogInformation("Application initialization completed successfully in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);

            return InitStartUpResult.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Application initialization was cancelled");
            return InitStartUpResult.FailedCancellation;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogCritical(ex, "Unexpected error during application initialization after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            return InitStartUpResult.UnexpectedError;
        }
    }

    private async Task<InitStartUpResult> EnsureDatabaseCreatedAsync(CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Creating database",
            async () => await context.Database.EnsureCreatedAsync(cancellationToken),
            InitStartUpResult.FailedCreateDatabase,
            cancellationToken);
    }

    private async Task<bool> IsDatabaseNewAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Checking if database is new...");
            var hasConfiguration = await context.BlogConfiguration.AnyAsync(cancellationToken);
            var isNew = !hasConfiguration;

            logger.LogInformation("Database is {DatabaseState}", isNew ? "new" : "existing");
            return isNew;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to determine if database is new, assuming existing");
            return false;
        }
    }

    private async Task<InitStartUpResult> SeedDatabaseAsync(CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Seeding database",
            async () =>
            {
                await context.ClearAllData();
                await Seed.SeedAsync(context, logger);
            },
            InitStartUpResult.FailedSeedingDatabase,
            cancellationToken);
    }

    private async Task<InitStartUpResult> InitializeBlogConfigurationAsync(bool isNew, CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Initializing blog configuration",
            async () => await blogConfigInitializer.Initialize(isNew),
            InitStartUpResult.FailedInitBlogConfig,
            cancellationToken);
    }

    private async Task<InitStartUpResult> MigrateDatabaseAsync(CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Migrating database",
            async () =>
            {
                var migrationResult = await migrationManager.TryMigrationAsync(context, cancellationToken);
                if (migrationResult.IsFailed)
                {
                    throw new InvalidOperationException($"Migration failed with result: {migrationResult}");
                }
            },
            InitStartUpResult.FailedDatabaseMigration,
            cancellationToken);
    }

    private async Task GenerateSiteIconsAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Generating site icons...");
            await siteIconInitializer.GenerateSiteIcons();
            logger.LogDebug("Site icons generated successfully");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Site icon generation was cancelled");
        }
        catch (Exception ex)
        {
            // Non-blocking operation - log but don't fail startup
            logger.LogWarning(ex, "Failed to generate site icons, but startup will continue");
        }
    }

    /// <summary>
    /// Executes a startup step with consistent logging and error handling.
    /// </summary>
    private async Task<InitStartUpResult> ExecuteStepAsync(
        string stepName,
        Func<Task> action,
        InitStartUpResult failureResult,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("Starting step: {StepName}", stepName);
            await action();
            logger.LogDebug("Completed step: {StepName}", stepName);
            return InitStartUpResult.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Step '{StepName}' was cancelled", stepName);
            throw; // Re-throw to be handled by caller
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to execute step: {StepName}", stepName);
            return failureResult;
        }
    }
}

public enum InitStartUpResult
{
    Success = 0,
    FailedCreateDatabase,
    FailedSeedingDatabase,
    FailedInitBlogConfig,
    FailedDatabaseMigration,
    FailedCancellation,
    UnexpectedError
}
