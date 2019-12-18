using System.Text.Encodings.Web;
using System.Web;

namespace Moonglade.HtmlCodec
{
    /// <summary>
    /// Html Encode / Decode that handles emoji correctly
    /// </summary>
    public class MoongladeHtmlCodec : IHtmlCodec
    {
        public string HtmlDecode(string encodedHtml)
        {
            return HttpUtility.HtmlDecode(encodedHtml);
        }

        public string HtmlEncode(string rawHtml)
        {
            return HtmlEncoder.Default.Encode(rawHtml);
        }
    }
}
