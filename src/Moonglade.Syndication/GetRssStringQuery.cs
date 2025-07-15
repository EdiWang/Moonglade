using LiteBus.Queries.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public record GetRssStringQuery(string CategoryName = null) : IQuery<string>;

public class GetRssStringQueryHandler : IQueryHandler<GetRssStringQuery, string>
{
    private readonly ISyndicationDataSource _sdds;
    private readonly FeedGenerator _feedGenerator;

    public GetRssStringQueryHandler(IBlogConfig blogConfig, ISyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
    {
        _sdds = sdds;

        var baseUrl = $"{httpContextAccessor.HttpContext!.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host}";

        _feedGenerator = new(
            baseUrl,
            blogConfig.GeneralSettings.SiteTitle,
            blogConfig.GeneralSettings.Description,
            Helper.FormatCopyright2Html(blogConfig.GeneralSettings.Copyright).Replace("&copy;", "©"),
            $"Moonglade v{Helper.AppVersion}",
            baseUrl,
            blogConfig.GeneralSettings.DefaultLanguageCode);
    }

    public async Task<string> HandleAsync(GetRssStringQuery request, CancellationToken ct)
    {
        var data = await _sdds.GetFeedDataAsync(request.CategoryName);
        if (data is null) return null;

        _feedGenerator.FeedItemCollection = data;
        var xml = await _feedGenerator.WriteRssAsync();
        return xml;
    }
}