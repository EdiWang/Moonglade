using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Models.Settings
{
    public class GeneralSettingsViewModel
    {
        [Required]
        [Display(Name = "Meta Keyword")]
        public string MetaKeyword { get; set; }

        [Required]
        [Display(Name = "Meta Description")]
        public string MetaDescription { get; set; }

        [Required]
        [Display(Name = "Logo Text")]
        public string LogoText { get; set; }

        [Required]
        [RegularExpression(@"[a-zA-Z0-9\s.\-\[\]]+", ErrorMessage = "Only letters, numbers, - and [] are allowed.")]
        [Display(Name = "Copyright")]
        public string Copyright { get; set; }

        [Required]
        [Display(Name = "Website Title")]
        public string SiteTitle { get; set; }

        [Required]
        [Display(Name = "Blogger Name")]
        public string BloggerName { get; set; }

        [Required]
        [Display(Name = "Blogger Description")]
        [DataType(DataType.MultilineText)]
        public string BloggerDescription { get; set; }

        [Required]
        [Display(Name = "Blogger Short Description")]
        public string BloggerShortDescription { get; set; }

        [Display(Name = "Side Bar Customized Html Pitch")]
        [DataType(DataType.MultilineText)]
        public string SideBarCustomizedHtmlPitch { get; set; }

        [Display(Name = "Footer Customized Html Pitch")]
        [DataType(DataType.MultilineText)]
        public string FooterCustomizedHtmlPitch { get; set; }

        [Display(Name = "Call-out Section Html Pitch")]
        [DataType(DataType.MultilineText)]
        public string CalloutSectionHtmlPitch { get; set; }

        [Display(Name = "Show Call-out Section")]
        public bool ShowCalloutSection { get; set; }

        public TimeSpan SelectedUtcOffset { get; set; }

        public string SelectedTimeZoneId { get; set; }

        public List<SelectListItem> TimeZoneList { get; set; }
    }
}
