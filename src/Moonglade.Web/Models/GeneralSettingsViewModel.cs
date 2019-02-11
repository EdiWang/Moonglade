using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class GeneralSettingsViewModel
    {
        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Blocked Words")]
        public string DisharmonyWords { get; set; }

        [Required]
        [Display(Name = "Meta Keyword")]
        public string MetaKeyword { get; set; }

        [Required]
        [Display(Name = "Meta Author")]
        public string MetaAuthor { get; set; }

        [Required]
        [Display(Name = "Website Title")]
        public string SiteTitle { get; set; }
    }
}
