using System;
using DateTimeOps;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers
{
    [HtmlTargetElement("pubdate", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class PubDateTagHelper : TagHelper
    {
        public DateTime? PubDateUtc { get; set; }

        public IDateTimeResolver DateTimeResolver { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "time";
            output.Attributes.SetAttribute("title", $"GMT {PubDateUtc}");
            output.Attributes.SetAttribute("datetime", PubDateUtc.GetValueOrDefault().ToString("u"));
            output.Content.SetContent(DateTimeResolver.ToTimeZone(PubDateUtc.GetValueOrDefault()).ToLongDateString());
        }
    }
}
