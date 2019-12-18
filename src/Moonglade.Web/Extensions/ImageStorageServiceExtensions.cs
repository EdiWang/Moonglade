using System;
using Microsoft.Extensions.DependencyInjection;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.ImageStorage.AzureBlob;
using Moonglade.ImageStorage.FileSystem;

namespace Moonglade.Web.Extensions
{
    public static class ImageStorageServiceExtensions
    {
        public static void AddMoongladeImageStorage(this IServiceCollection services, ImageStorageSettings imageStorage, string contentRootPath)
        {
            if (imageStorage.CDNSettings.GetImageByCDNRedirect)
            {
                if (string.IsNullOrWhiteSpace(imageStorage.CDNSettings.CDNEndpoint))
                {
                    throw new ArgumentNullException(nameof(imageStorage.CDNSettings.CDNEndpoint),
                        $"{nameof(imageStorage.CDNSettings.CDNEndpoint)} must be specified when {nameof(imageStorage.CDNSettings.GetImageByCDNRedirect)} is set to 'true'.");
                }

                // _logger.LogWarning("Images are configured to use CDN, the endpoint is out of control, use it on your own risk.");

                // Validate endpoint Url to avoid security risks
                // But it still has risks:
                // e.g. If the endpoint is compromised, the attacker could return any kind of response from a image with a big fuck to a script that can attack users.

                var endpoint = imageStorage.CDNSettings.CDNEndpoint;
                var isValidEndpoint = endpoint.IsValidUrl(Utils.UrlScheme.Https);
                if (!isValidEndpoint)
                {
                    throw new UriFormatException("CDN Endpoint is not a valid HTTPS Url.");
                }
            }

            if (null == imageStorage.Provider)
            {
                throw new ArgumentNullException("Provider", "Provider can not be null.");
            }

            var imageStorageProvider = imageStorage.Provider.ToLower();
            if (string.IsNullOrWhiteSpace(imageStorageProvider))
            {
                throw new ArgumentNullException("Provider", "Provider can not be empty.");
            }

            switch (imageStorageProvider)
            {
                case "azurestorage":
                    var conn = imageStorage.AzureStorageSettings.ConnectionString;
                    var container = imageStorage.AzureStorageSettings.ContainerName;
                    services.AddSingleton(s => new AzureStorageInfo(conn, container));
                    services.AddSingleton<IAsyncImageStorageProvider, AzureStorageImageProvider>();
                    break;
                case "filesystem":
                    var path = imageStorage.FileSystemSettings.Path;
                    var fullPath = Utils.ResolveImageStoragePath(contentRootPath, path);
                    services.AddSingleton(s => new FileSystemImageProviderInfo(fullPath));
                    services.AddSingleton<IAsyncImageStorageProvider, FileSystemImageProvider>();
                    break;
                default:
                    var msg = $"Provider {imageStorageProvider} is not supported.";
                    throw new NotSupportedException(msg);
            }
        }
    }
}
