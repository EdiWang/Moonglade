using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using Moonglade.Web.PagedList;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Pages.Admin;

public class PostModel(IMediator mediator) : PageModel
{
    private const int PageSize = 7;

    [BindProperty]
    [MaxLength(32)]
    public string SearchTerm { get; set; }

    public BasePagedList<PostSegment> PostSegments { get; set; }

    public async Task OnPost() => await GetPosts(1);

    public async Task OnGet(int pageIndex = 1, string searchTerm = null)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm)) SearchTerm = searchTerm;

        await GetPosts(pageIndex);
    }

    private async Task GetPosts(int pageIndex)
    {
        var (posts, totalRows) = await mediator.Send(new ListPostSegmentQuery(PostStatus.Published, (pageIndex - 1) * PageSize, PageSize, SearchTerm));
        PostSegments = new(posts, pageIndex, PageSize, totalRows);
    }
}