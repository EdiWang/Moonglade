using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin;

public class PostModel : PageModel
{
    private readonly IMediator _mediator;
    private const int PageSize = 7;

    [BindProperty]
    [MaxLength(32)]
    public string SearchTerm { get; set; }

    public StaticPagedList<PostSegment> PostSegments { get; set; }

    public PostModel(IMediator mediator) => _mediator = mediator;

    public async Task OnPost()
    {
        await GetPosts(1);
    }

    public async Task OnGet(int pageIndex = 1, string searchTerm = null)
    {
        await GetPosts(pageIndex, searchTerm);
    }

    private async Task GetPosts(int pageIndex, string searchTerm = null)
    {
        var (posts, totalRows) = await _mediator.Send(new ListPostSegmentQuery(PostStatus.Published, pageIndex * PageSize, PageSize, SearchTerm ?? searchTerm));
        PostSegments = new(posts, pageIndex, PageSize, totalRows);
    }
}