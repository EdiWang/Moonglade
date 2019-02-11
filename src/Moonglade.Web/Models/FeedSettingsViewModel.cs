using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Moonglade.Web.Models
{
    public class FeedSettingsViewModel
    {
        [Display(Name = "RSS Items")]
        public int RssItemCount { get; set; }

        [Required]
        [Display(Name = "Copyright")]
        public string RssCopyright { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string RssDescription { get; set; }

        [Required]
        [Display(Name = "RSS Generator Name")]
        public string RssGeneratorName { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string RssTitle { get; set; }

        [Required]
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; }

        public FeedSettingsViewModel()
        {
            RssItemCount = 20;
        }
    }
}
