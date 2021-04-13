using System.Collections.Generic;

namespace Moonglade.Web.Models
{
    public class SiteMapSettings
    {
        public string UrlSetNamespace { get; set; }

        public IDictionary<string, string> ChangeFreq { get; set; }
    }
}