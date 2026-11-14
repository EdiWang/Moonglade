using Edi.AspNetCore.Utils;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Http;
using Moonglade.Auth;
using Moonglade.Configuration;
using Moonglade.Utils;
using System.Text.Json.Serialization;

namespace Moonglade.Syndication;

public record GetAtomStringQuery(string Slug = null,
    [property: JsonIgnore] BlogSettingsSnapshot Snapshot = null,
    [property: JsonIgnore] OwnerProfile Owner = null,
    [property: JsonIgnore] OwnerPresentation Presentation = null) : IQuery<string>;

public class GetAtomStringQueryHandler(IBlogConfig blogConfig, SyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
    : IQueryHandler<GetAtomStringQuery, string>
{
    public async Task<string> HandleAsync(GetAtomStringQuery request, CancellationToken ct)
    {
        var settings = request.Snapshot ?? blogConfig.Capture();
        var baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";
        var generator = new FeedGenerator(
            baseUrl,
            settings.GeneralSettings.SiteTitle,
            (request.Presentation ?? OwnerPresentation.Default).Description,
            Helper.FormatCopyright2Html(settings.GeneralSettings.Copyright).Replace("&copy;", "©"),
            $"Moonglade v{VersionHelper.AppVersion}",
            baseUrl,
            settings.GeneralSettings.DefaultLanguageCode);
        var data = await sdds.GetFeedDataAsync(request.Slug, settings, ct);
        if (data is null) return null;

        data.ForEach(item => item.AuthorEmail = request.Owner?.Email);
        generator.FeedItemCollection = data;
        return await generator.WriteAtomAsync();
    }
}
