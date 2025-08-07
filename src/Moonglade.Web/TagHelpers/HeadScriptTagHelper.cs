using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Moonglade.Web.TagHelpers;

[HtmlTargetElement("head-script")]
public class HeadScriptTagHelper : TagHelper
{
    public string Src { get; set; }

    public string Integrity { get; set; }

    public string Crossorigin { get; set; }

    public bool Async { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "script";
        output.TagMode = TagMode.StartTagAndEndTag;

        if (!string.IsNullOrWhiteSpace(Src))
        {
            output.Attributes.SetAttribute("src", Src);
        }

        if (!string.IsNullOrWhiteSpace(Integrity))
        {
            output.Attributes.SetAttribute("integrity", Integrity);
        }

        if (!string.IsNullOrWhiteSpace(Crossorigin))
        {
            output.Attributes.SetAttribute("crossorigin", Crossorigin);
        }

        if (Async)
        {
            output.Attributes.SetAttribute("async", "async");
        }
    }
}
