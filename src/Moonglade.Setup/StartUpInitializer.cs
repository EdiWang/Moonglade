using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moonglade.Data;

namespace Moonglade.Setup;

public class StartUpInitializer(
    ILogger<StartUpInitializer> logger,
    BlogDbContext context,
    ConfigInitializer blogConfigInitializer,
    SiteIconBuilder siteIconInitializer)
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

            // An empty settings table alone does not identify a new installation.
            var isNewDatabase = await IsDatabaseEmptyAsync(cancellationToken);
            if (isNewDatabase)
            {
                result = await SeedDatabaseAsync(cancellationToken);
                if (result != InitStartUpResult.Success)
                    return result;
            }

            // Reload committed settings, or initialize missing groups on an existing installation.
            result = await InitializeBlogConfigurationAsync(false, cancellationToken);
            if (result != InitStartUpResult.Success)
                return result;

            // Step 4: Generate site icons (non-blocking operation)
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

    private async Task<bool> IsDatabaseEmptyAsync(CancellationToken ct)
    {
        return !await context.OwnerAvatar.AnyAsync(ct)
            && !await context.OwnerAccount.AnyAsync(ct)
            && !await context.OwnerOidc.AnyAsync(ct)
            && !await context.DataProtectionKeys.AnyAsync(ct)
            && !await context.BlogConfiguration.AnyAsync(ct)
            && !await context.Post.AnyAsync(ct)
            && !await context.Category.AnyAsync(ct)
            && !await context.Tag.AnyAsync(ct)
            && !await context.Comment.AnyAsync(ct)
            && !await context.CommentReply.AnyAsync(ct)
            && !await context.PostCategory.AnyAsync(ct)
            && !await context.PostTag.AnyAsync(ct)
            && !await context.PostView.AnyAsync(ct)
            && !await context.PostViewDaily.AnyAsync(ct)
            && !await context.BlogPage.AnyAsync(ct)
            && !await context.Mention.AnyAsync(ct)
            && !await context.BlogTheme.AnyAsync(ct)
            && !await context.StyleSheet.AnyAsync(ct)
            && !await context.BlogAsset.AnyAsync(ct)
            && !await context.Widget.AnyAsync(ct)
            && !await context.ActivityLog.AnyAsync(ct)
            && !await context.EmailOutboxMessage.AnyAsync(ct)
            && !await context.SiteVerificationFile.AnyAsync(ct);
    }

    private async Task<InitStartUpResult> SeedDatabaseAsync(CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Seeding database",
            async () =>
            {
                IDictionary<string, string> installedSettings = null;
                Guid installedPostId = default;
                var strategy = context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    context.ChangeTracker.Clear();
                    if (installedSettings is not null)
                    {
                        // A lost commit acknowledgment must not duplicate a committed installation.
                        var saved = await context.BlogConfiguration.AsNoTracking()
                            .ToDictionaryAsync(row => row.CfgKey, row => row.CfgValue, cancellationToken);
                        if (saved.Count == installedSettings.Count
                            && installedSettings.All(pair => saved.TryGetValue(pair.Key, out var json) && json == pair.Value)
                            && await context.Post.AnyAsync(post => post.Id == installedPostId, cancellationToken))
                        {
                            return;
                        }
                    }

                    if (!await IsDatabaseEmptyAsync(cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "The database contains existing or incomplete installation data. Preserve it and restore from a trusted backup before restarting.");
                    }

                    await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
                    await Seed.SeedAsync(context, logger, cancellationToken);
                    installedPostId = await context.Post.Select(post => post.Id).SingleAsync(cancellationToken);
                    installedSettings = (await blogConfigInitializer.InitializeStoreAsync(true, cancellationToken)).Values;
                    await transaction.CommitAsync(cancellationToken);
                });
            },
            InitStartUpResult.FailedSeedingDatabase,
            cancellationToken);
    }

    private async Task<InitStartUpResult> InitializeBlogConfigurationAsync(bool isNew, CancellationToken cancellationToken)
    {
        return await ExecuteStepAsync(
            "Initializing blog configuration",
            async () => await blogConfigInitializer.Initialize(isNew, cancellationToken),
            InitStartUpResult.FailedInitBlogConfig,
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
    FailedCancellation,
    UnexpectedError
}
