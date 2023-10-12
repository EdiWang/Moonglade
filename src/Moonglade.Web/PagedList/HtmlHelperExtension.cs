using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Web;

namespace Moonglade.Web.PagedList;

public static class HtmlHelperExtension
{
    public static HtmlString PagedListPager(this IHtmlHelper html,
    IPagedList list,
        Func<int, string> generatePageUrl,
        PagedListRenderOptions options)
    {
        var htmlHelper = new HtmlHelper(new TagBuilderFactory());
        var htmlString = htmlHelper.PagedListPager(list, generatePageUrl, options);

        htmlString = HttpUtility.HtmlDecode(htmlString);

        return new HtmlString(htmlString);
    }
}