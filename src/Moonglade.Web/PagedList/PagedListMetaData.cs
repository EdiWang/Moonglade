namespace Moonglade.Web.PagedList;

///<summary>
/// Non-enumerable version of the PagedList class.
///</summary>    
public class PagedListMetaData : IPagedList
{
    protected PagedListMetaData()
    {
    }

    ///<summary>
    /// Non-enumerable version of the PagedList class.
    ///</summary>
    ///<param name="pagedList">A PagedList (likely enumerable) to copy metadata from.</param>
    public PagedListMetaData(IPagedList pagedList)
    {
        PageCount = pagedList.PageCount;
        TotalItemCount = pagedList.TotalItemCount;
        PageNumber = pagedList.PageNumber;
        PageSize = pagedList.PageSize;
        HasPreviousPage = pagedList.HasPreviousPage;
        HasNextPage = pagedList.HasNextPage;
        IsFirstPage = pagedList.IsFirstPage;
        IsLastPage = pagedList.IsLastPage;
        FirstItemOnPage = pagedList.FirstItemOnPage;
        LastItemOnPage = pagedList.LastItemOnPage;
    }

    public int PageCount { get; protected set; }

    public int TotalItemCount { get; protected set; }

    public int PageNumber { get; protected set; }

    public int PageSize { get; protected set; }

    public bool HasPreviousPage { get; protected set; }

    public bool HasNextPage { get; protected set; }

    public bool IsFirstPage { get; protected set; }

    public bool IsLastPage { get; protected set; }

    /// <summary>
    /// One-based index of the first item in the paged subset, zero if the superset is empty or PageNumber
    /// is greater than PageCount.
    /// </summary>
    /// <value>
    /// One-based index of the first item in the paged subset, zero if the superset is empty or PageNumber
    /// is greater than PageCount.
    /// </value>
    public int FirstItemOnPage { get; protected set; }

    /// <summary>
    /// One-based index of the last item in the paged subset, zero if the superset is empty or PageNumber
    /// is greater than PageCount.
    /// </summary>
    /// <value>
    /// One-based index of the last item in the paged subset, zero if the superset is empty or PageNumber
    /// is greater than PageCount.
    /// </value>
    public int LastItemOnPage { get; protected set; }
}