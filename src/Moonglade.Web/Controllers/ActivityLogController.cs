#nullable enable

using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Localization;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using System.Text.Json;

namespace Moonglade.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ActivityLogController(
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<ActivityLogController> logger,
    IStringLocalizer<Program> localizer) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet("eventtypes")]
    public Task<IActionResult> GetEventTypes()
    {
        var groups = ActivityEvents.All
            .OrderBy(definition => definition.Group, StringComparer.Ordinal)
            .ThenBy(definition => definition.Key, StringComparer.Ordinal)
            .GroupBy(definition => definition.Group)
            .Select(group => new ActivityEventGroup(
                localizer[group.Key].Value,
                group.Select(definition => new ActivityEventOption(definition.Key, localizer[definition.Label].Value)).ToList()))
            .ToList();
        return Task.FromResult<IActionResult>(Ok(groups));
    }

    [HttpGet("list")]
    public async Task<IActionResult> List(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string[]? eventKeys = null,
        [FromQuery] DateTime? startTimeUtc = null,
        [FromQuery] DateTime? endTimeUtc = null)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        if (Request.Query.ContainsKey("eventTypes"))
        {
            ModelState.AddModelError("eventTypes", "Use eventKeys to filter activity events.");
        }
        if (eventKeys?.Any(key => !ActivityEvents.IsSupported(key)) == true)
        {
            ModelState.AddModelError(nameof(eventKeys), "Unsupported activity event key.");
        }
        if (startTimeUtc > endTimeUtc)
        {
            ModelState.AddModelError(nameof(endTimeUtc), "End time must not precede start time.");
        }
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var (logs, totalCount) = await queryMediator.QueryAsync(
            new ListActivityLogsQuery(pageSize, pageIndex, eventKeys, startTimeUtc, endTimeUtc));

        var items = logs.Select(log => new ActivityLogListItem(
            log.Id, log.EventKey, log.EventTimeUtc, log.ActorKind, Actor(log), Summary(log))).ToList();
        return Ok(new PagedResult<ActivityLogListItem>(items, pageIndex, pageSize, totalCount));
    }

    [HttpGet("{id:long}/metadata")]
    public async Task<IActionResult> GetMetadata([NotEmpty] long id)
    {
        var log = await queryMediator.QueryAsync(new GetMetaDataByActivityLogIdQuery(id));

        if (log == null) return NotFound();

        var changes = log.SettingChanges?.Select(change => new ActivityLogDetailChange(
            change.Key, localizer[DetailLabel(change.Key)].Value, change.OldValue, change.NewValue)).ToList();
        return Ok(new ActivityLogDetail(
            log.Id, log.EventKey, localizer[ActivityEvents.Get(log.EventKey).Label].Value,
            log.EventTimeUtc, log.ActorKind, Actor(log),
            log.TargetId, log.TargetDisplayName, log.IpAddress, log.UserAgent,
            log.Details.Where(field => field.Key != ActivityLogSettingChange.DetailsKey)
                .Select(field => new ActivityLogDetailField(
                    field.Key, localizer[DetailLabel(field.Key)].Value, field.Value)).ToList(),
            changes));
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> Clear()
    {
        var deletedCount = await CommandMediator.SendAsync(new ClearActivityLogsCommand());

        await LogActivityAsync(
            ActivityEvents.ActivityLogsCleared,
            null,
            ActivityLogDetails.Create(("DeletedCount", deletedCount)));

        return NoContent();
    }

    private string Actor(ActivityLogItem log) => log.ActorKind == ActivityActorKind.System
        ? localizer["System"].Value
        : string.IsNullOrWhiteSpace(log.ActorDisplayName) ? localizer["Administrator"].Value : log.ActorDisplayName;

    private string Summary(ActivityLogItem log)
    {
        var definition = ActivityEvents.Get(log.EventKey);
        var label = localizer[definition.Label].Value;
        if (definition.SettingKeys is not null) return label;

        var target = string.IsNullOrWhiteSpace(log.TargetDisplayName) ? log.TargetId : log.TargetDisplayName;
        return string.IsNullOrWhiteSpace(target) ? label : $"{label}: {target}";
    }

    private static string DetailLabel(string key) => key switch
    {
        "PostId" => "Post ID",
        "PageId" => "Page ID",
        "CategoryId" or "Id" => "Category ID",
        "CommentId" => "Comment ID",
        "CommentIds" => "Comment IDs",
        "TagId" => "Tag ID",
        "ThemeId" => "Theme ID",
        "WidgetId" => "Widget ID",
        "FileId" => "File ID",
        "Slug" => "Slug",
        "PostStatus" => "Post status",
        "Hours" => "Hours",
        "Count" => "Count",
        "DeletedCount" => "Deleted count",
        "WidgetType" => "Widget Type",
        "AccentColor" => "Accent Color",
        "FileName" => "File Name",
        "OldFileName" => "Previous file name",
        "FileSize" => "File size",
        "ContentType" => "Content Type",
        "ImageSize" => "Image dimensions",
        "SkipWatermark" => "Skip watermark",
        "SiteTitle" => "Site title",
        "LogoText" => "Logo text",
        "SideBarOption" => "Side bar display",
        "WidgetsProfile" => "Profile",
        "HotTagAmount" => "How many tags to show on sidebar",
        "WidgetsCategoryList" => "Categories",
        "WidgetsSubscriptionButtons" => "Subscription buttons",
        "UseGravatarAsProfilePicture" => "Use my Gravatar as profile picture",
        "DefaultLanguageCode" => "Default language",
        "UseDublinCoreMetaData" => "Use Dublin Core Metadata",
        "DefaultEditor" => "Default editor",
        "PostListPageSize" => "Post list page size",
        "PostTitleAlignment" => "Post title alignment",
        "ShowCalloutSection" => "Show call-out section",
        "ShowPostFooter" => "Show customize footer on each post",
        "DocumentOutline" => "Show post outline as side navigation",
        "EnableViewCount" => "Enable view count",
        "MaximumPageNumbersToDisplay" => "Maximum page numbers to display in pagination",
        "CommentProvider" => "Comment provider",
        "CommentOrder" => "Comments display order",
        "EnableComments" => "Enable comments",
        "EnableGravatar" => "Enable Gravatar in comment list",
        "EnableMarkdownLinks" => "Enable Markdown links in comments and replies",
        "RequireCommentReview" => "Require comment review",
        "CloseCommentAfterDays" => "Automatically close comments on posts older than x days",
        "EnableWordFilter" => "Enable word filter",
        "WordFilterMode" => "Word filter mode",
        "EnableCommentSectionOnMobile" => "Show comment section on mobile screens",
        "EnableEmailSending" => "Enable email sending",
        "SendEmailOnCommentReply" => "Send email on comment reply",
        "SendEmailOnNewComment" => "Send email on new comment",
        "FeedItemCount" => "Feed items",
        "UseFullContent" => "Use full blog post content instead of abstract",
        "EnableCDNRedirect" => "Enable CDN redirect",
        "IsWatermarkEnabled" => "Enable watermark",
        "KeepOriginImage" => "Keep origin image",
        "WatermarkFontSize" => "Font size",
        "WatermarkColorA" => "Watermark opacity",
        "WatermarkSkipPixel" => "Watermark skip pixel threshold",
        "EnableWebmention" => "Enable Webmention",
        "EnableOpenSearch" => "Enable OpenSearch",
        "EnableFoaf" => "Enable FOAF",
        "EnableOpml" => "Enable OPML",
        "EnableSiteMap" => "Enable Site Map",
        "EnablePostScheduler" => "Enable Post Scheduler",
        "WarnPostExternalLink" => "Show warning when clicking external links in posts",
        "WarnCommentExternalLink" => "Show warning when clicking external links in comments",
        "EnableJsonLdBlogPosting" => "Enable JSON-LD BlogPosting",
        "EnableCustomCss" => "Enable Custom CSS",
        "IsEnabled" => "Enabled",
        "IsPublished" => "Published",
        "Username" => "Username",
        _ => throw new InvalidDataException("Activity detail label is not defined.")
    };
}

public sealed record ActivityEventGroup(string Label, IReadOnlyList<ActivityEventOption> Items);
public sealed record ActivityEventOption(string Key, string Label);
public sealed record ActivityLogListItem(long Id, string EventKey, DateTime EventTimeUtc, ActivityActorKind ActorKind, string Actor, string Summary);
public sealed record ActivityLogDetail(
    long Id, string EventKey, string Heading, DateTime EventTimeUtc, ActivityActorKind ActorKind, string Actor,
    string? TargetId, string? TargetDisplayName, string? IpAddress, string? UserAgent, IReadOnlyList<ActivityLogDetailField> Fields,
    IReadOnlyList<ActivityLogDetailChange>? Changes = null);
public sealed record ActivityLogDetailField(string Key, string Label, JsonElement Value);
public sealed record ActivityLogDetailChange(string Key, string Label, JsonElement OldValue, JsonElement NewValue);
