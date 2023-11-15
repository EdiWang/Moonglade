namespace Moonglade.Web.Handlers;

public class SaveAssetToCdnHandler(IBlogImageStorage imageStorage, IBlogConfig blogConfig, IMediator mediator) : INotificationHandler<SaveAssetCommand>
{
    public async Task Handle(SaveAssetCommand request, CancellationToken ct)
    {
        // Currently only avatar
        var (assetId, assetBase64) = request;

        if (assetId != AssetId.AvatarBase64) return;

        if (blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var fileName = $"avatar-{AssetId.AvatarBase64.ToString("N")[..6]}.png";
            await imageStorage.DeleteAsync(fileName);
            fileName = await imageStorage.InsertAsync(fileName, Convert.FromBase64String(assetBase64));

            var random = new Random();
            blogConfig.GeneralSettings.AvatarUrl =
                blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName) + $"?{random.Next(100, 999)}";   //refresh local cache

            var kvp = blogConfig.UpdateAsync(blogConfig.GeneralSettings);
            await mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value), ct);
        }
    }
}