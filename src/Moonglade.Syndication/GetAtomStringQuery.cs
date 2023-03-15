using MediatR;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Utils;

namespace Moonglade.Syndication;

public record GetAtomStringQuery : IRequest<string>;

public class GetAtomStringQueryHandler : IRequestHandler<GetAtomStringQuery, string>
{
    private readonly ISyndicationDataSource _sdds;
    private readonly FeedGenerator _feedGenerator;

    public GetAtomStringQueryHandler(IBlogConfig blogConfig, ISyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
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

    public async Task<string> Handle(GetAtomStringQuery request, CancellationToken ct)
    {
        _feedGenerator.FeedItemCollection = await _sdds.GetFeedDataAsync();
        var xml = await _feedGenerator.WriteAtomAsync();
        return xml;
    }
}