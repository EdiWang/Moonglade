using LiteBus.Queries.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moonglade.Core;

namespace Moonglade.Setup;

public interface ISiteIconInitializer
{
    Task GenerateSiteIcons();
}

public class SiteIconInitializer(ILogger<SiteIconInitializer> logger, IQueryMediator queryMediator, IWebHostEnvironment env) : ISiteIconInitializer
{
    public async Task GenerateSiteIcons()
    {
        try
        {
            var iconData = await queryMediator.QueryAsync(new GetAssetQuery(AssetId.SiteIconBase64));
            MemoryStreamIconGenerator.GenerateIcons(iconData, env.WebRootPath, logger);
        }
        catch (Exception e)
        {
            // Non critical error, just log, do not block application start
            logger.LogError(e, e.Message);
        }
    }
}