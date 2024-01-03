using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PostFeature;
using Moonglade.Core.TagFeature;
using Moonglade.Web.PagedList;

namespace Moonglade.Web.Pages;

public class TagListModel(IMediator mediator, IBlogConfig blogConfig, ICacheAside cache) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int P { get; set; } = 1;
    public BasePagedList<PostDigest> Posts { get; set; }

    public async Task<IActionResult> OnGet(string normalizedName)
    {
        var tagResponse = await mediator.Send(new GetTagQuery(normalizedName));
        if (tagResponse is null) return NotFound();

        var pagesize = blogConfig.ContentSettings.PostListPageSize;
        var posts = await mediator.Send(new ListByTagQuery(tagResponse.Id, pagesize, P));
        var count = await cache.GetOrCreateAsync(BlogCachePartition.PostCountTag.ToString(), tagResponse.Id.ToString(), _ => mediator.Send(new CountPostQuery(CountType.Tag, TagId: tagResponse.Id)));

        ViewData["TitlePrefix"] = tagResponse.DisplayName;

        var list = new BasePagedList<PostDigest>(posts, P, pagesize, count);
        Posts = list;

        return Page();
    }
}