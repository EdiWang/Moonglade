using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Immutable;
using System.Text.Encodings.Web;

namespace Moonglade.Web.PagedList;

public class HtmlHelper
{
    #region Private methods

    private static string TagBuilderToString(TagBuilder tagBuilder)
    {
        var encoder = HtmlEncoder.Create(new TextEncoderSettings());

        using var writer = new StringWriter() as TextWriter;
        tagBuilder.WriteTo(writer, encoder);

        return writer.ToString();
    }

    private TagBuilder WrapInListItem(TagBuilder inner, params string[] classes)
    {
        var li = new TagBuilder("li");

        foreach (var @class in classes)
        {
            li.AddCssClass(@class);
        }

        li.InnerHtml.AppendHtml(TagBuilderToString(inner));

        return li;
    }

    private TagBuilder First(IPagedList list, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        const int targetPageNumber = 1;
        var first = new TagBuilder("a");

        first.InnerHtml.AppendHtml(string.Format(options.LinkToFirstPageFormat, targetPageNumber));

        foreach (var c in options.PageClasses ?? [])
        {
            first.AddCssClass(c);
        }

        if (list.IsFirstPage)
        {
            return WrapInListItem(first, "paged-list-skip-to-first", "disabled");
        }

        first.Attributes.Add("href", generatePageUrl(targetPageNumber));

        return WrapInListItem(first, "paged-list-skip-to-first");
    }

    private TagBuilder Previous(IPagedList list, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        var targetPageNumber = list.PageNumber - 1;
        var previous = new TagBuilder("a");

        previous.InnerHtml.AppendHtml(string.Format(options.LinkToPreviousPageFormat, targetPageNumber));

        previous.Attributes.Add("rel", "prev");

        foreach (var c in options.PageClasses ?? [])
        {
            previous.AddCssClass(c);
        }

        if (!list.HasPreviousPage)
        {
            return WrapInListItem(previous, options.PreviousElementClass, "disabled");
        }

        previous.Attributes.Add("href", generatePageUrl(targetPageNumber));

        return WrapInListItem(previous, options.PreviousElementClass);
    }

    private TagBuilder Page(int i, IPagedList list, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        string Format(int pageNumber) => string.Format("{0}", pageNumber);
        var targetPageNumber = i;
        var page = i == list.PageNumber
            ? new TagBuilder("span")
            : new TagBuilder("a");

        page.InnerHtml.SetContent(Format(targetPageNumber));

        foreach (var c in options.PageClasses ?? [])
        {
            page.AddCssClass(c);
        }

        if (i == list.PageNumber)
        {
            return WrapInListItem(page, "active");
        }

        page.Attributes.Add("href", generatePageUrl(targetPageNumber));

        return WrapInListItem(page);
    }

    private TagBuilder Next(IPagedList list, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        var targetPageNumber = list.PageNumber + 1;
        var next = new TagBuilder("a");

        next.InnerHtml.AppendHtml(string.Format(options.LinkToNextPageFormat, targetPageNumber));

        next.Attributes.Add("rel", "next");

        foreach (var c in options.PageClasses ?? [])
        {
            next.AddCssClass(c);
        }

        if (!list.HasNextPage)
        {
            return WrapInListItem(next, options.NextElementClass, "disabled");
        }

        next.Attributes.Add("href", generatePageUrl(targetPageNumber));

        return WrapInListItem(next, options.NextElementClass);
    }

    private TagBuilder Last(IPagedList list, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        var targetPageNumber = list.PageCount;
        var last = new TagBuilder("a");

        last.InnerHtml.AppendHtml(string.Format(options.LinkToLastPageFormat, targetPageNumber));

        foreach (var c in options.PageClasses ?? [])
        {
            last.AddCssClass(c);
        }

        if (list.IsLastPage)
        {
            return WrapInListItem(last, "paged-list-skip-to-last", "disabled");
        }

        last.Attributes.Add("href", generatePageUrl(targetPageNumber));

        return WrapInListItem(last, "paged-list-skip-to-last");
    }

    private TagBuilder PageCountAndLocationText(IPagedList list, PagedListRenderOptions options)
    {
        var text = new TagBuilder("a");

        text.InnerHtml.SetContent(string.Format(options.PageCountAndCurrentLocationFormat, list.PageNumber, list.PageCount));

        return WrapInListItem(text, "PagedList-pageCountAndLocation", "disabled");
    }

    #endregion Private methods

    public string PagedListPager(IPagedList pagedList, Func<int, string> generatePageUrl, PagedListRenderOptions options)
    {
        var list = pagedList ?? new BasePagedList<int>(ImmutableList<int>.Empty, 1, 10, 0);

        if (list.PageCount <= 1)
        {
            return null;
        }

        var listItemLinks = new List<TagBuilder>();

        //calculate start and end of range of page numbers
        var firstPageToDisplay = 1;
        var lastPageToDisplay = list.PageCount;
        var pageNumbersToDisplay = lastPageToDisplay;

        if (options.MaximumPageNumbersToDisplay.HasValue && list.PageCount > options.MaximumPageNumbersToDisplay)
        {
            // cannot fit all pages into pager
            var maxPageNumbersToDisplay = options.MaximumPageNumbersToDisplay.Value;

            firstPageToDisplay = list.PageNumber - maxPageNumbersToDisplay / 2;

            if (firstPageToDisplay < 1)
            {
                firstPageToDisplay = 1;
            }

            pageNumbersToDisplay = maxPageNumbersToDisplay;
            lastPageToDisplay = firstPageToDisplay + pageNumbersToDisplay - 1;

            if (lastPageToDisplay > list.PageCount)
            {
                firstPageToDisplay = list.PageCount - maxPageNumbersToDisplay + 1;
            }
        }

        //first
        if (firstPageToDisplay > 1)
        {
            listItemLinks.Add(First(list, generatePageUrl, options));
        }

        //previous
        if (!list.IsFirstPage)
        {
            listItemLinks.Add(Previous(list, generatePageUrl, options));
        }

        //text
        if (options.DisplayPageCountAndCurrentLocation)
        {
            listItemLinks.Add(PageCountAndLocationText(list, options));
        }

        //page
        listItemLinks.AddRange(Enumerable.Range(firstPageToDisplay, pageNumbersToDisplay).Select(i => Page(i, list, generatePageUrl, options)));

        //next
        if (!list.IsLastPage)
        {
            listItemLinks.Add(Next(list, generatePageUrl, options));
        }

        //last
        if (lastPageToDisplay < list.PageCount)
        {
            listItemLinks.Add(Last(list, generatePageUrl, options));
        }

        if (listItemLinks.Any())
        {
            //append classes to all list item links
            foreach (var li in listItemLinks)
            {
                li.AddCssClass("page-item");
            }
        }

        //collapse all of the list items into one big string
        var listItemLinksString = listItemLinks.Aggregate(
            new StringBuilder(),
            (sb, listItem) => sb.Append(TagBuilderToString(listItem)),
            sb => sb.ToString());

        var ul = new TagBuilder("ul");

        ul.InnerHtml.AppendHtml(listItemLinksString);

        foreach (var c in options.UlElementClasses ?? [])
        {
            ul.AddCssClass(c);
        }

        var outerDiv = new TagBuilder("div");

        outerDiv.AddCssClass("pagination-container");

        outerDiv.InnerHtml.AppendHtml(TagBuilderToString(ul));

        return TagBuilderToString(outerDiv);
    }
}