namespace Moonglade.Configuration
{
    public class CustomStyleSheetSettings : IBlogSettings
    {
        public bool EnableCustomCss { get; set; }

        public string CssCode { get; set; }
    }
}
