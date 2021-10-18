using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("metadesc", TagStructure = TagStructure.NormalOrSelfClosing)]
public class MetaDescriptionTagHelper : TagHelper
{
    public string Description { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "meta";
        output.Attributes.SetAttribute("name", "description");
        output.Attributes.SetAttribute("content", Description.Trim());
    }
}