namespace Moonglade.HtmlEncoding
{
    public interface IHtmlCodec
    {
        string HtmlDecode(string encodedHtml);

        string HtmlEncode(string rawHtml);
    }
}
