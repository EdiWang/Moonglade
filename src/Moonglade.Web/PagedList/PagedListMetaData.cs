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
    }

    public int PageCount { get; protected set; }

    public int TotalItemCount { get; protected set; }

    public int PageNumber { get; protected set; }

    public int PageSize { get; protected set; }

    public bool HasPreviousPage { get; protected set; }

    public bool HasNextPage { get; protected set; }

    public bool IsFirstPage { get; protected set; }

    public bool IsLastPage { get; protected set; }
}