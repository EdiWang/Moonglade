namespace Moonglade.HtmlCodec
{
    public interface IHtmlCodec
    {
        string HtmlDecode(string encodedHtml);

        string HtmlEncode(string rawHtml, bool attributeEncode = false);
    }
}
