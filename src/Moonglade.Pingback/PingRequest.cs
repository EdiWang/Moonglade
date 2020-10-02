namespace Moonglade.Pingback
{
    public class PingRequest
    {
        public string SourceUrl { get; set; }

        public string TargetUrl { get; set; }

        public string Title { get; set; }

        public bool ContainsHtml { get; set; }

        public bool SourceHasLink { get; set; }
    }
}