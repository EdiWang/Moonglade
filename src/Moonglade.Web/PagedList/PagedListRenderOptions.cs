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
        LinkToNextPageFormat = ">";
        LinkToLastPageFormat = ">>";
        PageCountAndCurrentLocationFormat = "Page {0} of {1}.";
        ItemSliceAndTotalFormat = "Showing items {0} through {1} of {2}.";
        UlElementClasses = ["pagination"];
        PageClasses = ["page-link"];
        PreviousElementClass = "paged-list-skip-to-previous";
        NextElementClass = "paged-list-skip-to-next";
    }

    public IEnumerable<string> UlElementClasses { get; set; }
    public IEnumerable<string> PageClasses { get; set; }
    public string PreviousElementClass { get; set; }
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
    public string LinkToFirstPageFormat { get; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the previous page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "&lt; Previous"
    ///</example>
    public string LinkToPreviousPageFormat { get; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the next page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Next &gt;"
    ///</example>
    public string LinkToNextPageFormat { get; }

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the last page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Last &gt;&gt;"
    ///</example>
    public string LinkToLastPageFormat { get; }

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