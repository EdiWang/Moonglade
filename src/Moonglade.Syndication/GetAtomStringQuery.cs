﻿using MediatR;
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

        var acc = httpContextAccessor.HttpContext ?? throw new ArgumentNullException($"{nameof(httpContextAccessor)}.HttpContext is null.");
        var baseUrl = $"{acc.Request.Scheme}://{acc.Request.Host}";

        _feedGenerator = new(
            baseUrl,
            blogConfig.FeedSettings.RssTitle,
            blogConfig.GeneralSettings.Description,
            blogConfig.FeedSettings.RssCopyright,
            $"Moonglade v{Helper.AppVersion}",
            baseUrl);
    }

    public async Task<string> Handle(GetAtomStringQuery request, CancellationToken cancellationToken)
    {
        _feedGenerator.FeedItemCollection = await _sdds.GetFeedDataAsync();
        var xml = await _feedGenerator.WriteAtomAsync();
        return xml;
    }
}