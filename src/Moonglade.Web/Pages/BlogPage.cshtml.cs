using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Core.PageFeature;

namespace Moonglade.Web.Pages;

public class BlogPageModel : PageModel
{
    private readonly IMediator _mediator;

    private readonly ICacheAside _cache;
    private readonly IConfiguration _configuration;
    public BlogPage BlogPage { get; set; }

    public BlogPageModel(
        IMediator mediator, ICacheAside cache, IConfiguration configuration)
    {
        _cache = cache;
        _configuration = configuration;
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var page = await _cache.GetOrCreateAsync(BlogCachePartition.Page.ToString(), slug.ToLower(), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(_configuration["CacheSlidingExpirationMinutes:Page"]));

            var p = await _mediator.Send(new GetPageBySlugQuery(slug));
            return p;
        });

        if (page is null || !page.IsPublished) return NotFound();

        BlogPage = page;
        return Page();
    }
}