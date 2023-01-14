using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using System.ComponentModel.DataAnnotations;
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

    public async Task OnPost() => await GetPosts(1);

    public async Task OnGet(int pageIndex = 1, string searchTerm = null)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm)) SearchTerm = searchTerm;

        await GetPosts(pageIndex);
    }

    private async Task GetPosts(int pageIndex)
    {
        var (posts, totalRows) = await _mediator.Send(new ListPostSegmentQuery(PostStatus.Published, (pageIndex - 1) * PageSize, PageSize, SearchTerm));
        PostSegments = new(posts, pageIndex, PageSize, totalRows);
    }
}