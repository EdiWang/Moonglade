namespace Moonglade.Pingback
{
    public class PingRequest
    {
        public string SourceIpAddress { get; set; }

        public string SourceUrl { get; set; }

        public string TargetUrl { get; set; }

        public SourceDocumentInfo SourceDocumentInfo { get; set; }
    }
}