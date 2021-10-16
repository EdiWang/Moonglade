using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration
{
    public class CustomStyleSheetSettings : IBlogSettings
    {
        [Display(Name = "Enable Custom CSS")]
        public bool EnableCustomCss { get; set; }

        [MaxLength(10240)]
        public string CssCode { get; set; }
    }
}
