using System;
using System.Collections.Generic;
using System.Text;

namespace Moonglade.Core
{
    public interface IHtmlCodec
    {
        string HtmlDecode(string encodedHtml);

        string HtmlEncode(string rawHtml);
    }
}
