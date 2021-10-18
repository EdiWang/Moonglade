using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Core.PageFeature;

namespace Moonglade.Web.Pages;

public class BlogPageModel : PageModel
{
    private readonly IMediator _mediator;

    private readonly IBlogCache _cache;
    private readonly AppSettings _settings;
    public BlogPage BlogPage { get; set; }

    public BlogPageModel(
        IMediator mediator, IBlogCache cache, IOptions<AppSettings> settingsOptions)
    {
        _cache = cache;
        _mediator = mediator;
        _settings = settingsOptions.Value;
    }

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return BadRequest();

        var page = await _cache.GetOrCreateAsync(CacheDivision.Page, slug.ToLower(), async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Page"]);

            var p = await _mediator.Send(new GetPageBySlugQuery(slug));
            return p;
        });

        if (page is null || !page.IsPublished) return NotFound();

        BlogPage = page;
        return Page();
    }
}