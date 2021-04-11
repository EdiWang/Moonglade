namespace Moonglade.Configuration
{
    public class FeedSettings : IBlogSettings
    {
        public int RssItemCount { get; set; }
        public string RssCopyright { get; set; }
        public string RssDescription { get; set; }
        public string RssTitle { get; set; }
        public string AuthorName { get; set; }
        public bool UseFullContent { get; set; }
    }
}