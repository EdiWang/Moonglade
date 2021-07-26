using System.Collections.Generic;

namespace Moonglade.Web.Models.Settings
{
    public class BlogTheme
    {
        public string Key { get; set; }
        public IDictionary<string, string> CssRules { get; set; }
    }
}
