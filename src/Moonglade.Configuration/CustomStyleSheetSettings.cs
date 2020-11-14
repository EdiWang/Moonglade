using Moonglade.Configuration.Abstraction;

namespace Moonglade.Configuration
{
    public class CustomStyleSheetSettings : BlogSettings
    {
        public bool EnableCustomCss { get; set; }

        public string CssCode { get; set; }
    }
}
