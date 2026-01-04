namespace Moonglade.Web.PagedList;

public class PagedListRenderOptions
{
    ///<summary>
    /// The maximum number of page numbers to display. Null displays all page numbers.
    ///</summary>
    public int MaximumPageNumbersToDisplay { get; set; } = 10;

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the first page. The one-based index of the page (always 1 in this case) is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "&lt;&lt; First"
    ///</example>
    public string LinkToFirstPageFormat { get; } = "<<";

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the previous page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "&lt; Previous"
    ///</example>
    public string LinkToPreviousPageFormat { get; } = "<";

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the next page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Next &gt;"
    ///</example>
    public string LinkToNextPageFormat { get; } = ">";

    ///<summary>
    /// The pre-formatted text to display inside the hyperlink to the last page. The one-based index of the page is passed into the formatting function - use {0} to reference it.
    ///</summary>
    ///<example>
    /// "Last &gt;&gt;"
    ///</example>
    public string LinkToLastPageFormat { get; } = ">>";

    public IEnumerable<string> UlElementClasses { get; set; } = ["pagination"];

    public IEnumerable<string> PageClasses { get; set; } = ["page-link"];

    public string PreviousElementClass { get; set; } = "paged-list-skip-to-previous";

    public string NextElementClass { get; set; } = "paged-list-skip-to-next";
}