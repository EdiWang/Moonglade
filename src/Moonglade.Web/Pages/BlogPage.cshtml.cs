using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Features.Page;

namespace Moonglade.Web.Pages;

public class BlogPageModel(IQueryMediator queryMediator, ICacheAside cache, IConfiguration configuration) : PageModel
{
    public PageEntity BlogPage { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var page = await cache.GetOrCreateAsync(BlogCachePartition.Page.ToString(), slug.ToLower(), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["Page:CacheMinutes"]!));

            var p = await queryMediator.QueryAsync(new GetPageBySlugQuery(slug));
            return p;
        });

        if (page is null || !page.IsPublished) return NotFound();

        BlogPage = page;

        if (page.UpdateTimeUtc.HasValue && bool.Parse(configuration["Page:LastModifiedHeader"]!))
        {
            Response.Headers.LastModified = page.UpdateTimeUtc.Value.ToString("R");

            if (Request.Headers.TryGetValue("If-Modified-Since", out var ifModifiedSince))
            {
                if (DateTime.TryParse(ifModifiedSince, out var ifModifiedSinceDate) &&
                    page.UpdateTimeUtc.Value <= ifModifiedSinceDate)
                {
                    return StatusCode(304);
                }
            }
        }

        return Page();
    }
}