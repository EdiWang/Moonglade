using System.Collections.Generic;

namespace Moonglade.Configuration.Settings
{
    public class SiteMapSettings
    {
        public string UrlSetNamespace { get; set; }

        public Dictionary<string, string> ChangeFreq { get; set; }
    }
}