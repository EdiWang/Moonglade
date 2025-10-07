using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Data.Entities;
using Moonglade.Features.PostFeature;

namespace Moonglade.Web.Pages;

public class PostModel(IConfiguration configuration, IQueryMediator queryMediator, ICommandMediator commandMediator) : PageModel
{
    public PostEntity Post { get; set; }

    public PostViewEntity PostView { get; set; }

    public bool IsViewCountEnabled { get; } = configuration.GetValue<bool>("Post:EnableViewCount");

    public async Task<IActionResult> OnGetAsync(int year, int month, int day, string slug)
    {
        if (year > DateTime.UtcNow.Year || string.IsNullOrWhiteSpace(slug)) return NotFound();

        var post = await queryMediator.QueryAsync(new GetPostBySlugQuery(year, month, day, slug));

        if (post is null) return NotFound();

        Post = post;
        ViewData["TitlePrefix"] = $"{Post.Title}";

        if (IsViewCountEnabled)
        {
            await commandMediator.SendAsync(new AddRequestCountCommand(post.Id));
            PostView = await queryMediator.QueryAsync(new GetPostViewQuery(post.Id));
        }

        return Page();
    }
}