namespace Moonglade.Web.Handlers;

public class SaveAssetToCdnHandler : INotificationHandler<SaveAssetCommand>
{
    private readonly IBlogImageStorage _imageStorage;
    private readonly IBlogConfig _blogConfig;
    private readonly IMediator _mediator;

    public SaveAssetToCdnHandler(IBlogImageStorage imageStorage, IBlogConfig blogConfig, IMediator mediator)
    {
        _imageStorage = imageStorage;
        _blogConfig = blogConfig;
        _mediator = mediator;
    }

    public async Task Handle(SaveAssetCommand request, CancellationToken ct)
    {
        // Currently only avatar
        var (assetId, assetBase64) = request;

        if (assetId != AssetId.AvatarBase64) return;

        if (_blogConfig.ImageSettings.EnableCDNRedirect)
        {
            var fileName = $"avatar-{AssetId.AvatarBase64.ToString("N")[..6]}.png";
            await _imageStorage.DeleteAsync(fileName);
            fileName = await _imageStorage.InsertAsync(fileName, Convert.FromBase64String(assetBase64));

            var random = new Random();
            _blogConfig.GeneralSettings.AvatarUrl =
                _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName) + $"?{random.Next(100, 999)}";   //refresh local cache

            var kvp = _blogConfig.UpdateAsync(_blogConfig.GeneralSettings);
            await _mediator.Send(new UpdateConfigurationCommand(kvp.Key, kvp.Value), ct);
        }
    }
}