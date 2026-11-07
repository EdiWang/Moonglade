using System.Text.Json.Serialization;
using Edi.AspNetCore.Utils;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public record GetRssStringQuery(string CategoryName = null, [property: JsonIgnore] BlogSettingsSnapshot Snapshot = null) : IQuery<string>;

public class GetRssStringQueryHandler(IBlogConfig blogConfig, SyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetRssStringQuery, string>
{
    public async Task<string> HandleAsync(GetRssStringQuery request, CancellationToken ct)
    {
        var settings = request.Snapshot ?? blogConfig.Capture();
        var baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
        var generator = new FeedGenerator(
            baseUrl,
            settings.GeneralSettings.SiteTitle,
            settings.GeneralSettings.Description,
            Helper.FormatCopyright2Html(settings.GeneralSettings.Copyright).Replace("&copy;", "©"),
            $"Moonglade v{VersionHelper.AppVersion}",
            baseUrl,
            settings.GeneralSettings.DefaultLanguageCode);
        var data = await sdds.GetFeedDataAsync(request.CategoryName, settings, ct);
        if (data is null) return null;
        generator.FeedItemCollection = data;
        return await generator.WriteRssAsync();
    }
}
