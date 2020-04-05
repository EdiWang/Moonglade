using System.Collections.Generic;

namespace Moonglade.OpmlFileWriter
{
    public class OpmlInfo
    {
        public string HtmlUrl { get; set; }

        public string XmlUrl { get; set; }

        public string SiteTitle { get; set; }

        public string CategoryXmlUrlTemplate { get; set; }

        public string CategoryHtmlUrlTemplate { get; set; }

        public IEnumerable<OpmlCategoryInfo> CategoryInfo { get; set; }
    }
}