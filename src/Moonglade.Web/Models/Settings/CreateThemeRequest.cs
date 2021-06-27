using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models.Settings
{
    public class CreateThemeRequest
    {
        [Required]
        [MaxLength(32)]
        public string Name { get; set; }

        [Required]
        public string AccentColor1 { get; set; }

        [Required]
        public string AccentColor2 { get; set; }

        [Required]
        public string AccentColor3 { get; set; }
    }
}
