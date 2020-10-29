using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class CustomStyleSheetSettingsViewModel
    {
        [Display(Name = "Enable Custom CSS")]
        public bool EnableCustomCss { get; set; }

        [MaxLength(10240)]
        public string CssCode { get; set; }
    }
}