using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers
{
    [HtmlTargetElement("rss", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class RssTagHelper : TagHelper
    {
        public string Title { get; set; }

        public string Href { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "link";
            output.Attributes.SetAttribute("type", new HtmlString("application/rss+xml"));
            output.Attributes.SetAttribute("rel", "alternate");
            output.Attributes.SetAttribute("title", Title);
            output.Attributes.SetAttribute("href", Href);
        }
    }
}
