using MediatR;
using Microsoft.AspNetCore.Http;
using Moonglade.Configuration;
using Moonglade.Utils;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Syndication
{
    public class GetAtomStringQuery : IRequest<string>
    {

    }

    public class GetAtomStringQueryHandler : IRequestHandler<GetAtomStringQuery, string>
    {
        private readonly ISyndicationDataSource _sdds;
        private readonly FeedGenerator _feedGenerator;

        public GetAtomStringQueryHandler(IBlogConfig blogConfig, ISyndicationDataSource sdds, IHttpContextAccessor httpContextAccessor)
        {
            _sdds = sdds;

            var acc = httpContextAccessor;
            var baseUrl = $"{acc.HttpContext.Request.Scheme}://{acc.HttpContext.Request.Host}";

            _feedGenerator = new()
            {
                HostUrl = baseUrl,
                HeadTitle = blogConfig.FeedSettings.RssTitle,
                HeadDescription = blogConfig.GeneralSettings.MetaDescription,
                Copyright = blogConfig.FeedSettings.RssCopyright,
                Generator = $"Moonglade v{Helper.AppVersion}",
                TrackBackUrl = baseUrl
            };
        }

        public async Task<string> Handle(GetAtomStringQuery request, CancellationToken cancellationToken)
        {
            _feedGenerator.FeedItemCollection = await _sdds.GetFeedDataAsync();
            var xml = await _feedGenerator.WriteAtomAsync();
            return xml;
        }
    }
}
