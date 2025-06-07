using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("opensearch", TagStructure = TagStructure.NormalOrSelfClosing)]
public class OpenSearchTagHelper : TagHelper
{
    public string Title { get; set; }
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
        output.Attributes.SetAttribute("type", new HtmlString("application/opensearchdescription+xml"));
        output.Attributes.SetAttribute("rel", "search");
        output.Attributes.SetAttribute("title", Title.Trim());
        output.Attributes.SetAttribute("href", Href.Trim());
    }
}