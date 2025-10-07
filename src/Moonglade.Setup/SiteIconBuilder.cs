using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Features;

namespace Moonglade.Setup;

public interface ISiteIconBuilder
{
    Task GenerateSiteIcons();
}

public class SiteIconBuilder(ILogger<SiteIconBuilder> logger, IQueryMediator queryMediator, IWebHostEnvironment env) : ISiteIconBuilder
{
    public async Task GenerateSiteIcons()
    {
        try
        {
            var iconData = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.SiteIconBase64));
            InMemoryIconGenerator.GenerateIcons(iconData, env.WebRootPath, logger);
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            logger.LogError(e, "Error generating site icons: {Message}", e.Message);
        }
    }
}