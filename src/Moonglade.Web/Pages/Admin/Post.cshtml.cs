using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Spec;
using X.PagedList;

namespace Moonglade.Web.Pages.Admin;

public class PostModel : PageModel
{
    private readonly IMediator _mediator;

    [MaxLength(32)]
    public string SearchTerm { get; set; }

    public StaticPagedList<PostSegment> PostSegments { get; set; }

    public PostModel(IMediator mediator) => _mediator = mediator;

    public async Task OnGet(int pageIndex = 1)
    {
        const int pageSize = 8;
        var (posts, totalRows) = await _mediator.Send(new ListPostSegmentQuery(PostStatus.Published, pageIndex * pageSize, pageSize, SearchTerm));

        PostSegments = new(posts, pageIndex, pageSize, totalRows);
    }
}