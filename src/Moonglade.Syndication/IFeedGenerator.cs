namespace Moonglade.Syndication
{
    public interface IFeedGenerator
    {
        string Copyright { get; set; }
        string Generator { get; set; }
        string HeadDescription { get; set; }
        string HeadTitle { get; set; }
        string HostUrl { get; set; }
        IEnumerable<FeedEntry> FeedItemCollection { get; set; }
        string TrackBackUrl { get; set; }
    }
}
