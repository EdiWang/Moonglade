using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;

namespace Moonglade.Web.PagedList;

public class PagedListRenderOptions
{
    ///<summary>
    /// The default settings render all navigation links and no descriptive text
    ///</summary>
    public PagedListRenderOptions()
    {
        HtmlEncoder = HtmlEncoder.Default;
        DisplayLinkToIndividualPages = true;
        DisplayPageCountAndCurrentLocation = false;
        MaximumPageNumbersToDisplay = 10;
        DisplayEllipsesWhenNotShowingAllPageNumbers = true;
        EllipsesFormat = "&#8230;";
        LinkToFirstPageFormat = "<<";
        LinkToPreviousPageFormat = "<";
        LinkToIndividualPageFormat = "{0}";
        LinkToNextPageFormat = ">";
        LinkToLastPageFormat = ">>";
        PageCountAndCurrentLocationFormat = "Page {0} of {1}.";
        ItemSliceAndTotalFormat = "Showing items {0} through {1} of {2}.";
        ItemSliceAndTotalPosition = ItemSliceAndTotalPosition.Start;
        FunctionToDisplayEachPageNumber = null;
        ContainerDivClasses = new[] { "pagination-container" };
        UlElementClasses = new[] { "pagination" };
        LiElementClasses = new[] { "page-item" };
        PageClasses = new[] { "page-link" };
        ActiveLiElementClass = "active";
        EllipsesElementClass = "paged-list-ellipses";
        PreviousElementClass = "PagedList-skip-to-previous";
        NextElementClass = "paged-list-skip-to-next";
    }

    /// <summary>
    /// Gets or sets the HtmlEncoder to use encoding HTML render.
    /// </summary>
    public HtmlEncoder HtmlEncoder { get; set; }

    ///<summary>
    /// CSS Classes to append to the &lt;div&gt; element that wraps the paging control.
    ///</summary>
    public IEnumerable<string> ContainerDivClasses { get; set; }

    ///<summary>
    /// CSSClasses to append to the &lt;ul&gt; element in the paging control.
    ///</summary>
    public IEnumerable<string> UlElementClasses { get; set; }

    ///<summary>
    /// CSS Classes to append to every &lt;li&gt; element in the paging control.
    ///</summary>
    public IEnumerable<string> LiElementClasses { get; set; }

    /// <summary>
    /// CSS Classes to appent to active &lt;li&gt; element in the paging control.
    /// </summary>
    public string ActiveLiElementClass { get; set; }

    ///<summary>
    /// CSS Classes to append to every &lt;a&gt; or &lt;span&gt; element that represent each page in the paging control.
    ///</summary>
    public IEnumerable<string> PageClasses { get; set; }

    ///<summary>
    /// CSS Classes to append to previous element in the paging control.
    ///</summary>
    public string PreviousElementClass { get; set; }

    ///<summary>
    /// CSS Classes to append to next element in the paging control.
    ///</summary>
    public string NextElementClass { get; set; }

    ///<summary>
    /// CSS Classes to append to Ellipses element in the paging control.
    ///</summary>
    public string EllipsesElementClass { get; set; }

    ///<summary>
    /// When true, includes hyperlinks for each page in the list.
    ///</summary>
    public bool DisplayLinkToIndividualPages { get; set; }

    ///<summary>
    /// When true, shows the current page number and the total number of pages in the list.
    ///</summary>
    ///<example>
    /// "Page 3 of 8."
    ///</example>
    public bool DisplayPageCountAndCurrentLocation { get; set; }

    ///<summary>
    /// When true, shows the one-based index of the first and last items on the page, and the total number of items in the list.
    ///</summary>
    ///<example>
    /// "Showing items 75 through 100 of 183."
    ///</example>
    public bool DisplayItemSliceAndTotal { get; set; }

    ///<summary>
    /// The maximum number of page numbers to display. Null displays all page numbers.
    ///</summary>
    public int? MaximumPageNumbersToDisplay { get; set; }

    ///<summary>
    /// If true, adds an ellipsis where not all page numbers are being displayed.
    ///</summary>
    ///<example>
    /// "1 2 3 4 5 ...",
    /// "... 6 7 8 9 10 ...",
    /// "... 11 12 13 14 15"
    ///</example>
    public bool DisplayEllipsesWhenNotShowingAllPageNumbers { get; set; }

    ///<summary>
    /// The pre-formatted text to display when not all page numbers are displayed at once.
    ///</summary>
    ///<example>
    /// "..."
    ///</example>
    public string EllipsesFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the first page. The one-based index of the page (always 1 in this case) is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "&lt;&lt; First"
    ///</example>
    public string LinkToFirstPageFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the previous page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "&lt; Previous"
    ///</example>
    public string LinkToPreviousPageFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to each individual page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "{0}"
    ///</example>
    public string LinkToIndividualPageFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the next page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Next &gt;"
    ///</example>
    public string LinkToNextPageFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the last page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Last &gt;&gt;"
    ///</example>
    public string LinkToLastPageFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display when DisplayPageCountAndCurrentLocation is true. Use {0} to reference the current page and {1} to reference the total number of pages.
    ///</summary>
    ///<example>
    /// "Page {0} of {1}."
    ///</example>
    public string PageCountAndCurrentLocationFormat { get; set; }

    ///<summary>
    /// The pre-formatted text to display when DisplayItemSliceAndTotal is true. Use {0} to reference the first item on the page, {1} for the last item on the page, and {2} for the total number of items across all pages.
    ///</summary>
    ///<example>
    /// "Showing items {0} through {1} of {2}."
    ///</example>
    public string ItemSliceAndTotalFormat { get; set; }

    /// <summary>
    /// If set to <see cref="ItemSliceAndTotalPosition.Start"/>, render an info at the beginning of the list. If set to <see cref="ItemSliceAndTotalPosition.End"/>, render the at the beginning of the list.
    /// </summary>
    public ItemSliceAndTotalPosition ItemSliceAndTotalPosition { get; set; }

    /// <summary>
    /// A function that will render each page number when specified (and DisplayLinkToIndividualPages is true). If no function is specified, the LinkToIndividualPageFormat value will be used instead.
    /// </summary>
    public Func<int, string> FunctionToDisplayEachPageNumber { get; set; }

    /// <summary>
    /// Text that will appear between each page number. If null or whitespace is specified, no delimiter will be shown.
    /// </summary>
    public string DelimiterBetweenPageNumbers { get; set; }

    /// <summary>
    /// An extension point which allows you to fully customize the anchor tags used for clickable pages, as well as navigation features such as Next, Last, etc.
    /// </summary>
    public Func<TagBuilder, TagBuilder, TagBuilder> FunctionToTransformEachPageLink { get; set; }
}