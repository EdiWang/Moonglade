using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Moonglade.Features.Asset;

namespace Moonglade.Web.Handlers;

public class SaveAssetToCdnHandler(
    ILogger<SaveAssetToCdnHandler> logger,
    IBlogImageStorage imageStorage,
    IBlogConfig blogConfig,
    ICommandMediator commandMediator) : IEventHandler<SaveAssetEvent>
{
    public async Task HandleAsync(SaveAssetEvent request, CancellationToken ct)
    {
        // Only process avatar asset
        if (request.AssetId != AssetId.AvatarBase64)
            return;

        if (!blogConfig.ImageSettings.EnableCDNRedirect)
            return;

        if (string.IsNullOrWhiteSpace(request.AssetBase64))
        {
            logger.LogWarning("AssetBase64 is null or empty.");
            return;
        }

        // Generate avatar file name based on AssetId
        var fileName = $"avatar-{AssetId.AvatarBase64.ToString("N")[..6]}.png";

        // Delete the old avatar file if exists
        await imageStorage.DeleteAsync(fileName);

        // Save the new avatar image
        var imageBytes = Convert.FromBase64String(request.AssetBase64);
        fileName = await imageStorage.InsertAsync(fileName, imageBytes);

        // Update the CDN URL with cache-busting query
        var cacheBuster = Random.Shared.Next(100, 999);
        var cdnUrl = $"{blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName)}?{cacheBuster}";
        blogConfig.GeneralSettings.AvatarUrl = cdnUrl;

        // Persist the new configuration
        var (key, value) = blogConfig.UpdateAsync(blogConfig.GeneralSettings);
        await commandMediator.SendAsync(new UpdateConfigurationCommand(key, value), ct);

        logger.LogInformation("Avatar updated and saved to CDN. URL: {AvatarUrl}", cdnUrl);
    }
}
