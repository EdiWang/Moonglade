namespace Moonglade.Syndication;

public class OpmlDoc
{
    public string HtmlUrl { get; set; }

    public string XmlUrl { get; set; }

    public string SiteTitle { get; set; }

    public string XmlUrlTemplate { get; set; }

    public string HtmlUrlTemplate { get; set; }

    public IEnumerable<KeyValuePair<string, string>> ContentInfo { get; set; }
}