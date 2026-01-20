using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Features.Asset;

namespace Moonglade.Setup;

public interface ISiteIconBuilder
{
    Task GenerateSiteIcons();
    Task RegenerateSiteIcons(string base64Data);
}

public class SiteIconBuilder(ILogger<SiteIconBuilder> logger, IQueryMediator queryMediator, IWebHostEnvironment env) : ISiteIconBuilder
{
    public async Task GenerateSiteIcons()
    {
        try
        {
            // Try to load from file system cache first
            if (TryLoadIconsFromCache())
            {
                logger.LogInformation("Site icons loaded from file system cache");
                return;
            }

            // Generate from database and cache to file system
            logger.LogInformation("Generating site icons from database...");
            var iconData = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.SiteIconBase64));
            await RegenerateSiteIcons(iconData);
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            logger.LogError(e, "Error generating site icons: {Message}", e.Message);
        }
    }

    public async Task RegenerateSiteIcons(string base64Data)
    {
        try
        {
            // Clear existing icons
            InMemoryIconGenerator.ClearIcons();

            // Generate new icons in memory
            InMemoryIconGenerator.GenerateIcons(base64Data, env.WebRootPath, logger);

            // Persist to file system cache
            await PersistIconsToCache();

            logger.LogInformation("Site icons regenerated and cached to file system");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error regenerating site icons: {Message}", e.Message);
            throw;
        }
    }

    private bool TryLoadIconsFromCache()
    {
        try
        {
            var cacheDir = InMemoryIconGenerator.GetSiteIconCacheDirectory();

            if (!Directory.Exists(cacheDir))
            {
                logger.LogDebug("Cache directory does not exist: {CacheDir}", cacheDir);
                return false;
            }

            var iconFiles = Directory.GetFiles(cacheDir, "*.png");
            if (iconFiles.Length == 0)
            {
                logger.LogDebug("No cached icon files found in {CacheDir}", cacheDir);
                return false;
            }

            // Load all icons from cache
            foreach (var filePath in iconFiles)
            {
                var fileName = Path.GetFileName(filePath);
                var bytes = File.ReadAllBytes(filePath);
                InMemoryIconGenerator.LoadIcon(fileName, bytes);
            }

            logger.LogDebug("Loaded {Count} icons from cache directory: {CacheDir}", iconFiles.Length, cacheDir);
            return true;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to load icons from cache, will regenerate");
            return false;
        }
    }

    private async Task PersistIconsToCache()
    {
        try
        {
            var cacheDir = InMemoryIconGenerator.GetSiteIconCacheDirectory();

            // Clear old cache files
            if (Directory.Exists(cacheDir))
            {
                foreach (var file in Directory.GetFiles(cacheDir, "*.png"))
                {
                    File.Delete(file);
                }
            }

            // Save all icons to cache
            foreach (var (fileName, bytes) in InMemoryIconGenerator.SiteIconDictionary)
            {
                var filePath = Path.Combine(cacheDir, fileName);
                await File.WriteAllBytesAsync(filePath, bytes);
            }

            logger.LogDebug("Persisted {Count} icons to cache directory: {CacheDir}",
                InMemoryIconGenerator.SiteIconDictionary.Count, cacheDir);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to persist icons to cache, will continue with in-memory only");
        }
    }
}