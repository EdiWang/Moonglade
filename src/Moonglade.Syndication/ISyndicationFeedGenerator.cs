using System.Collections.Generic;

namespace Edi.SyndicationFeedGenerator
{
    public interface ISyndicationFeedGenerator
    {
        string Copyright { get; set; }
        string Generator { get; set; }
        string HeadDescription { get; set; }
        string HeadTitle { get; set; }
        string HostUrl { get; set; }
        IEnumerable<SimpleFeedItem> FeedItemCollection { get; set; }
        int MaxContentLength { get; set; }
        string TrackBackUrl { get; set; }
    }
}
