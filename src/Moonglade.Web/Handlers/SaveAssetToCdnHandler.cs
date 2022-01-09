namespace Moonglade.Web.Handlers
{
    public class SaveAssetToCdnHandler : INotificationHandler<SaveAssetCommand>
    {
        private readonly IBlogImageStorage _imageStorage;
        private readonly IBlogConfig _blogConfig;


        public SaveAssetToCdnHandler(IBlogImageStorage imageStorage,
            IBlogConfig blogConfig)
        {
            _imageStorage = imageStorage;
            _blogConfig = blogConfig;
        }

        public async Task Handle(SaveAssetCommand request, CancellationToken cancellationToken)
        {
            // Currently only avatar
            if (request.AssetId != AssetId.AvatarBase64)
            { 
                return; 
            }

            if (_blogConfig.ImageSettings.EnableCDNRedirect)
            {
                var fileName = $"avatar-{AssetId.AvatarBase64.ToString("N")}.png";
                await _imageStorage.DeleteAsync(fileName);
                fileName = await _imageStorage.InsertAsync(fileName, Convert.FromBase64String(request.AssetBase64));
                _blogConfig.GeneralSettings.AvatarUrl = _blogConfig.ImageSettings.CDNEndpoint.CombineUrl(fileName);
                await _blogConfig.SaveAsync(_blogConfig.GeneralSettings);
            }
        }
    }
}
