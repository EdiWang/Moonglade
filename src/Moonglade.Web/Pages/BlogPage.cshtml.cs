using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Pages;

public class BlogPageModel(IMediator mediator, ICacheAside cache, IConfiguration configuration) : PageModel
{
    public PageEntity BlogPage { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var page = await cache.GetOrCreateAsync(BlogCachePartition.Page.ToString(), slug.ToLower(), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["CacheSlidingExpirationMinutes:Page"]!));

            var p = await mediator.Send(new GetPageBySlugQuery(slug));
            return p;
        });

        if (page is null || !page.IsPublished) return NotFound();

        BlogPage = page;
        return Page();
    }
}