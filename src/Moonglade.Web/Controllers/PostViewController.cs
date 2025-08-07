﻿using LiteBus.Commands.Abstractions;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostViewController(IConfiguration configuration, IBlogConfig blogConfig, ICommandMediator commandMediator) : ControllerBase
{
    private readonly bool _isEnabled = configuration.GetValue<bool>("Post:EnableViewCount");

    private static readonly HashSet<string> KnownBots = new(StringComparer.OrdinalIgnoreCase)
    {
        "Google", "Bingbot", "Baidu", "YandexBot", "Sogou", "Exabot", "ia_archiver",
        "facebookexternalhit", "Twitterbot", "rogerbot", "linkedinbot", "embedly",
        "showyoubot", "outbrain", "pinterest", "slackbot", "vkShare", "W3C_Validator",
        "redditbot", "Applebot", "WhatsApp", "flipboard", "tumblr", "bitlybot",
        "SkypeUriPreview", "nuzzel", "Discordbot", "Qwantify", "pinterestbot",
        "TelegramBot", "Chrome-Lighthouse", "DuckDuckGo", "DuckDuckBot", "Slack"
    };

    [HttpPost]
    public async Task<IActionResult> AddViewCount([FromBody] ViewRequest request)
    {
        if (!_isEnabled) return NotFound();

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
        await commandMediator.SendAsync(new AddViewCountCommand(postId, ip));
    }

    private static bool IsKnownBot(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return false;
        }

        return KnownBots.Any(bot => userAgent.Contains(bot, StringComparison.OrdinalIgnoreCase));
    }
}

public class ViewRequest
{
    [NotEmpty]
    public Guid PostId { get; set; }

    [Required]
    public DateTime ClientTimeStamp { get; set; }
}
