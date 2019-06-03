using System.Web;

namespace Moonglade.Core
{
    public class HttpUtilityHtmlCodec : IHtmlCodec
    {
        public string HtmlDecode(string encodedHtml)
        {
            return HttpUtility.HtmlDecode(encodedHtml);
        }

        public string HtmlEncode(string rawHtml)
        {
            return HttpUtility.HtmlEncode(rawHtml);
        }
    }
}