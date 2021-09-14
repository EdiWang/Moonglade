using System.ComponentModel.DataAnnotations;

namespace Moonglade.Theme
{
    public class CreateThemeRequest
    {
        [Required]
        [MaxLength(32)]
        public string Name { get; set; }

        [Required]
        [RegularExpression(@"(#([\da-f]{3}){1,2}|(rgb|hsl)a\((\d{1,3}%?,\s?){3}(1|0?\.\d+)\)|(rgb|hsl)\(\d{1,3}%?(,\s?\d{1,3}%?){2}\))")]
        public string AccentColor1 { get; set; }

        [Required]
        [RegularExpression(@"(#([\da-f]{3}){1,2}|(rgb|hsl)a\((\d{1,3}%?,\s?){3}(1|0?\.\d+)\)|(rgb|hsl)\(\d{1,3}%?(,\s?\d{1,3}%?){2}\))")]
        public string AccentColor2 { get; set; }

        [Required]
        [RegularExpression(@"(#([\da-f]{3}){1,2}|(rgb|hsl)a\((\d{1,3}%?,\s?){3}(1|0?\.\d+)\)|(rgb|hsl)\(\d{1,3}%?(,\s?\d{1,3}%?){2}\))")]
        public string AccentColor3 { get; set; }
    }
}
