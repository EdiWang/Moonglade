using System.Collections.Generic;

namespace Moonglade.Syndication
{
    public class OpmlDoc
    {
        public string HtmlUrl { get; set; }

        public string XmlUrl { get; set; }

        public string SiteTitle { get; set; }

        public string CategoryXmlUrlTemplate { get; set; }

        public string CategoryHtmlUrlTemplate { get; set; }

        public IEnumerable<OpmlCategory> CategoryInfo { get; set; }
    }
}