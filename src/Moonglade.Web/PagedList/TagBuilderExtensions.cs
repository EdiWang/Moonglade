using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;

namespace Moonglade.Web.PagedList;

public static class TagBuilderExtensions
{
    public static string ToString(this TagBuilder tagBuilder, TagRenderMode renderMode, HtmlEncoder encoder = null)
    {
        encoder ??= HtmlEncoder.Create(new TextEncoderSettings());

        using var writer = new StringWriter() as TextWriter;
        tagBuilder.WriteTo(writer, encoder);

        return writer.ToString();
    }
}