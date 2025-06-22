using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moonglade.Web.Handlers;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("foaf", TagStructure = TagStructure.NormalOrSelfClosing)]
public class FoafTagHelper : TagHelper
{
    public string Href { get; set; }
    public bool Enabled { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(Href))
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "link";
        output.Attributes.SetAttribute("type", new HtmlString(WriteFoafCommand.ContentType));
        output.Attributes.SetAttribute("rel", "meta");
        output.Attributes.SetAttribute("title", "FOAF");
        output.Attributes.SetAttribute("href", Href);
    }
}