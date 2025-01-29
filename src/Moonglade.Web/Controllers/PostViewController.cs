﻿using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostViewController(IBlogConfig blogConfig, IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> AddViewCount([FromBody] ViewRequest request)
    {
        var referer = Request.Headers.Referer.ToString();
        if (string.IsNullOrEmpty(referer) || !referer.Contains(blogConfig.GeneralSettings.CanonicalPrefix))
        {
            return BadRequest();
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        if (IsKnownBot(userAgent))
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
        await mediator.Send(new AddViewCountCommand(postId, ip));
    }

    private bool IsKnownBot(string userAgent)
    {
        var bots = new[]
        {
            "Google",
            "Bingbot",
            "Baidu",
            "YandexBot",
            "Sogou",
            "Exabot",
            "ia_archiver",
            "facebookexternalhit",
            "Twitterbot",
            "rogerbot",
            "linkedinbot",
            "embedly",
            "showyoubot",
            "outbrain",
            "pinterest",
            "slackbot",
            "vkShare",
            "W3C_Validator",
            "redditbot",
            "Applebot",
            "WhatsApp",
            "flipboard",
            "tumblr",
            "bitlybot",
            "SkypeUriPreview",
            "nuzzel",
            "Discordbot",
            "Google Page Speed",
            "Qwantify",
            "pinterestbot",
            "bitlybot",
            "TelegramBot",
            "Chrome-Lighthouse",
            "DuckDuckGo-Favicons-Bot",
            "DuckDuckBot",
            "Slack-ImgProxy",
            "Slackbot"
        };

        return bots.Any(b => userAgent.Contains(b, StringComparison.OrdinalIgnoreCase));
    }
}

public class ViewRequest
{
    [NotEmpty]
    public Guid PostId { get; set; }

    [Required]
    public DateTime ClientTimeStamp { get; set; }
}
