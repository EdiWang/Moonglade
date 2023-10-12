using System.Linq.Expressions;

namespace Moonglade.Web.PagedList;

/// <summary>
/// Represents a subset of a collection of objects that can be individually accessed by index and containing
/// metadata about the superset collection of objects this subset was created from.
/// </summary>
/// <remarks>
/// Represents a subset of a collection of objects that can be individually accessed by index and containing
/// metadata about the superset collection of objects this subset was created from.
/// </remarks>
/// <typeparam name="T">The type of object the collection should contain.</typeparam>
/// <seealso cref="IPagedList{T}"/>
/// <seealso cref="BasePagedList{T}"/>
/// <seealso cref="StaticPagedList{T}"/>
/// <seealso cref="List{T}"/>    
public class PagedList<T> : BasePagedList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class that divides the supplied superset
    /// into subsets the size of the supplied pageSize. The instance then only contains the objects contained
    /// in the subset specified by index.
    /// </summary>
    /// <param name="superset">
    /// The collection of objects to be divided into subsets. If the collection
    /// implements <see cref="IQueryable{T}"/>, it will be treated as such.
    /// </param>
    /// <param name="pageNumber">
    /// The one-based index of the subset of objects to be contained by this instance.
    /// </param>
    /// <param name="pageSize">The maximum size of any individual subset.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified index cannot be less than zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The specified page size cannot be less than one.</exception>
    public PagedList(IQueryable<T> superset, int pageNumber, int pageSize)
        : base(pageNumber, pageSize, superset?.Count() ?? 0)
    {
        if (TotalItemCount > 0 && superset != null)
        {
            var skip = (pageNumber - 1) * pageSize;

            Subset.AddRange(superset.Skip(skip).Take(pageSize));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PagedList{T}"/> class that divides the supplied superset
    /// into subsets the size of the supplied pageSize. The instance then only contains the objects contained in
    /// the subset specified by index.
    /// </summary>
    /// <param name="superset">
    /// The collection of objects to be divided into subsets. If the collection
    /// implements <see cref="IQueryable{T}"/>, it will be treated as such.
    /// </param>
    /// <param name="pageNumber">The one-based index of the subset of objects to be contained by this instance.</param>
    /// <param name="pageSize">The maximum size of any individual subset.</param>
    /// <exception cref="ArgumentOutOfRangeException">The specified index cannot be less than zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The specified page size cannot be less than one.</exception>
    public PagedList(IEnumerable<T> superset, int pageNumber, int pageSize)
        : this(superset.AsQueryable<T>(), pageNumber, pageSize)
    {
    }
}