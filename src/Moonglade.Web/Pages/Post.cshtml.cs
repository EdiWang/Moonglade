using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Features.Post;

namespace Moonglade.Web.Pages;

public class PostModel(
    IConfiguration configuration,
    IBlogConfig blogConfig,
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator) : PageModel
{
    public PostEntity Post { get; set; }

    public PostViewEntity PostView { get; set; }

    public bool IsViewCountEnabled => blogConfig.ContentSettings.EnableViewCount;

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var routeLink = $"{year}/{month}/{day}/{slug}".ToLower();

        var psm = await cache.GetOrCreateAsync(BlogCachePartition.Post.ToString(), $"{routeLink}", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(int.Parse(configuration["PostCacheMinutes"]!));

            var post = await queryMediator.QueryAsync(new GetPostBySlugQuery(routeLink));
            return post;
        });

        if (psm is null) return NotFound();

        Post = psm;
        ViewData["TitlePrefix"] = $"{Post.Title}";

        if (IsViewCountEnabled)
        {
            await commandMediator.SendAsync(new AddRequestCountCommand(psm.Id));
            PostView = await queryMediator.QueryAsync(new GetPostViewQuery(psm.Id));
        }

        return Page();
    }
}