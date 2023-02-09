using MediatR;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public record GetRssStringQuery(string CategoryName = null) : IRequest<string>;

public class GetRssStringQueryHandler : IRequestHandler<GetRssStringQuery, string>
{
    private readonly ISyndicationDataSource _sdds;
    private readonly FeedGenerator _feedGenerator;

    public GetRssStringQueryHandler(IBlogConfig blogConfig, ISyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
    {
        _sdds = sdds;

        var acc = httpContextAccessor;
        var baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";

        _feedGenerator = new(
            baseUrl,
            blogConfig.GeneralSettings.SiteTitle,
            blogConfig.GeneralSettings.Description,
            Helper.FormatCopyright2Html(blogConfig.GeneralSettings.Copyright).Replace("&copy;", "©"),
            $"Moonglade v{Helper.AppVersion}",
            baseUrl,
            blogConfig.GeneralSettings.DefaultLanguageCode);
    }

    public async Task<string> Handle(GetRssStringQuery request, CancellationToken ct)
    {
        var data = await _sdds.GetFeedDataAsync(request.CategoryName);
        if (data is null) return null;

        _feedGenerator.FeedItemCollection = data;
        var xml = await _feedGenerator.WriteRssAsync();
        return xml;
    }
}