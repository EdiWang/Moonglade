namespace Moonglade.Web.PagedList;

public class PagedListRenderOptions
{
    ///<summary>
    /// The default settings render all navigation links and no descriptive text
    ///</summary>
    public PagedListRenderOptions()
    {
        DisplayPageCountAndCurrentLocation = false;
        MaximumPageNumbersToDisplay = 10;
        LinkToFirstPageFormat = "<<";
        LinkToPreviousPageFormat = "<";
        LinkToIndividualPageFormat = "{0}";
        LinkToNextPageFormat = ">";
        LinkToLastPageFormat = ">>";
        PageCountAndCurrentLocationFormat = "Page {0} of {1}.";
        ItemSliceAndTotalFormat = "Showing items {0} through {1} of {2}.";
        UlElementClasses = ["pagination"];
        PageClasses = ["page-link"];
        PreviousElementClass = "PagedList-skip-to-previous";
        NextElementClass = "paged-list-skip-to-next";
    }

    ///<summary>
    /// CSSClasses to append to the &lt;ul&gt; element in the paging control.
    ///</summary>
    public IEnumerable<string> UlElementClasses { get; set; }

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
    /// When true, shows the current page number and the total number of pages in the list.
    ///</summary>
    ///<example>
    /// "Page 3 of 8."
    ///</example>
    public bool DisplayPageCountAndCurrentLocation { get; set; }

    ///<summary>
    /// The maximum number of page numbers to display. Null displays all page numbers.
    ///</summary>
    public int? MaximumPageNumbersToDisplay { get; set; }

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
}