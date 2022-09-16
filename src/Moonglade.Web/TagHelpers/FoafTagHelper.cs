using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("foaf", TagStructure = TagStructure.NormalOrSelfClosing)]
public class FoafTagHelper : TagHelper
{
    public string Href { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "link";
        output.Attributes.SetAttribute("type", new HtmlString(WriteFoafCommand.ContentType));
        output.Attributes.SetAttribute("rel", "meta");
        output.Attributes.SetAttribute("title", "FOAF");
        output.Attributes.SetAttribute("href", Href);
    }
}