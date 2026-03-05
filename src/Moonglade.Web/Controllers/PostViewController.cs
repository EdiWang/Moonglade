using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Features.Post;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostViewController(
    IBlogConfig blogConfig,
    ICommandMediator commandMediator,
    IMemoryCache memoryCache,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddViewCount([FromBody] ViewRequest request)
    {
        if (!blogConfig.ContentSettings.EnableViewCount) return NotFound();

        var userAgent = Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent)) return NoContent();

        // UA blacklist filter
        var crawlerKeywords = configuration.GetSection("ViewCount:CrawlerUserAgents").Get<string[]>() ?? [];
        var uaLower = userAgent.ToLowerInvariant();
        if (crawlerKeywords.Any(keyword => uaLower.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return NoContent();
        }

        // IP + UA dedup via IMemoryCache
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var deduplicationMinutes = configuration.GetValue("ViewCount:DeduplicationMinutes", 60);
        var cacheKey = $"pv:{request.PostId}:{ip}:{userAgent.GetHashCode()}";

        if (memoryCache.TryGetValue(cacheKey, out _))
        {
            return NoContent();
        }

        memoryCache.Set(cacheKey, true, TimeSpan.FromMinutes(deduplicationMinutes));

        await commandMediator.SendAsync(new AddViewCountCommand(request.PostId));

        return NoContent();
    }
}

public class ViewRequest
{
    [NotEmpty]
    public Guid PostId { get; set; }
}
