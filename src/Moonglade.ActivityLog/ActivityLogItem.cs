using Moonglade.Data.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moonglade.ActivityLog;

public class ActivityLogItem
{
    public long Id { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public DateTime EventTimeUtc { get; set; }
    public ActivityActorKind ActorKind { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? TargetId { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public IReadOnlyDictionary<string, JsonElement> Details { get; set; } = new Dictionary<string, JsonElement>();
    [JsonIgnore]
    public IReadOnlyList<ActivityLogSettingChangeDetail>? SettingChanges { get; init; }

    internal static ActivityLogItem FromEntity(ActivityLogEntity entity)
    {
        var definition = ActivityEvents.Get(entity.EventKey);
        if (!Enum.IsDefined(entity.ActorKind)) throw new InvalidDataException("Stored activity has an unsupported actor kind.");
        var isSystem = entity.ActorKind == ActivityActorKind.System;
        var details = ActivityLogDetails.Parse(definition, entity.Details);
        return new()
        {
            Id = entity.Id,
            EventKey = definition.Key,
            EventTimeUtc = entity.EventTimeUtc,
            ActorKind = entity.ActorKind,
            ActorDisplayName = isSystem ? null : entity.ActorDisplayName,
            TargetId = entity.TargetId,
            TargetDisplayName = entity.TargetDisplayName,
            IpAddress = isSystem ? null : entity.IpAddress,
            UserAgent = isSystem ? null : entity.UserAgent,
            Details = details.Details,
            SettingChanges = details.SettingChanges
        };
    }
}
