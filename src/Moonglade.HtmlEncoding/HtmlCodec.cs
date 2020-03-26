using System.Text.Encodings.Web;
using System.Web;
using Moonglade.HtmlCodec;

namespace Moonglade.HtmlEncoding
{
    /// <summary>
    /// Html Encode / Decode that handles emoji correctly
    /// </summary>
    public class HtmlCodec : IHtmlCodec
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
