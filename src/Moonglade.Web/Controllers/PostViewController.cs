using LiteBus.Commands.Abstractions;
using Moonglade.Features.Post;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostViewController(IBlogConfig blogConfig, ICommandMediator commandMediator) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> AddViewCount([FromBody] ViewRequest request)
    {
        if (!blogConfig.ContentSettings.EnableViewCount) return NotFound();

        var referer = Request.Headers.Referer.ToString();
        if (string.IsNullOrEmpty(referer))
        {
            return BadRequest();
        }

        var uri = new Uri(referer);
        var canonicalPrefix = blogConfig.GeneralSettings.CanonicalPrefix;
        if (!uri.IsLocalhostUrl() && !string.IsNullOrWhiteSpace(canonicalPrefix) && !referer.Contains(canonicalPrefix))
        {
            return BadRequest();
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return Forbid();
        }

        var now = DateTime.UtcNow;
        if (Math.Abs((now - request.ClientTimeStamp).TotalMinutes) > 5)
        {
            return BadRequest();
        }

        await SaveViewRecord(request.PostId, HttpContext.Connection.RemoteIpAddress?.ToString());

        return NoContent();
    }

    private async Task SaveViewRecord(Guid postId, string ip)
    {
        await commandMediator.SendAsync(new AddViewCountCommand(postId, ip));
    }
}

public class ViewRequest
{
    [NotEmpty]
    public Guid PostId { get; set; }

    [Required]
    public DateTime ClientTimeStamp { get; set; }
}
