using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers
{
    [HtmlTargetElement("rsd", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class RSDTagHelper : TagHelper
    {
        public string Href { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "link";
            output.Attributes.SetAttribute("type", new HtmlString("application/rsd+xml"));
            output.Attributes.SetAttribute("rel", "edituri");
            output.Attributes.SetAttribute("title", "RSD");
            output.Attributes.SetAttribute("href", Href);
        }
    }
}
