using System.Web;

namespace Moonglade.HtmlCodec
{
    public class HttpUtilityHtmlCodec : IHtmlCodec
    {
        public string HtmlDecode(string encodedHtml)
        {
            return HttpUtility.HtmlDecode(encodedHtml);
        }

        public string HtmlEncode(string rawHtml, bool attributeEncode = false)
        {
            if (attributeEncode)
            {
                return HttpUtility.HtmlAttributeEncode(rawHtml);
            }
            return HttpUtility.HtmlEncode(rawHtml);
        }
    }
}