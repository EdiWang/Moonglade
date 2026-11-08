using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using System.Text.Json.Serialization;

namespace Moonglade.ActivityLog;

public record CreateActivityLogCommand(
    string EventKey,
    ActivityActorKind ActorKind,
    string? ActorDisplayName,
    string? TargetId = null,
    string? TargetDisplayName = null,
    IReadOnlyDictionary<string, object?>? Details = null,
    string? IpAddress = null,
    string? UserAgent = null,
    DateTime? EventTimeUtc = null) : ICommand
{
    [JsonIgnore]
    public string? TraceIdentifier { get; init; }
}

public class CreateActivityLogCommandHandler(
    BlogDbContext db,
    ILogger<CreateActivityLogCommandHandler> logger) : ICommandHandler<CreateActivityLogCommand>
{
    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public async Task HandleAsync(CreateActivityLogCommand request, CancellationToken ct)
    {
        ActivityLogEntity? entity = null;
        try
        {
            var definition = ActivityEvents.Get(request.EventKey);
            if (!Enum.IsDefined(request.ActorKind))
            {
                throw new ArgumentException("Unsupported activity actor kind.");
            }

            var isSystem = request.ActorKind == ActivityActorKind.System;
            entity = new ActivityLogEntity
            {
                EventKey = definition.Key,
                EventTimeUtc = request.EventTimeUtc ?? DateTime.UtcNow,
                ActorKind = request.ActorKind,
                ActorDisplayName = isSystem ? null : TruncateString(request.ActorDisplayName, 100),
                TargetId = TruncateString(request.TargetId, 100),
                TargetDisplayName = TruncateString(request.TargetDisplayName, 200),
                Details = ActivityLogDetails.Serialize(definition, request.Details),
                IpAddress = isSystem ? null : TruncateString(request.IpAddress, 50),
                UserAgent = isSystem ? null : TruncateString(request.UserAgent, 512)
            };

            await db.ActivityLog.AddAsync(entity, ct);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Activity recorded for {EventKey} with ID {ActivityLogId}", definition.Key, entity.Id);
        }
        catch (Exception e)
        {
            if (entity is not null)
            {
                db.Entry(entity).State = EntityState.Detached;
            }

            var safeEventKey = ActivityEvents.IsSupported(request.EventKey) ? request.EventKey : "(unsupported)";
            if (string.IsNullOrEmpty(request.TraceIdentifier))
            {
                logger.LogError("Activity logging failed for {EventKey}. Failure type: {FailureType}",
                    safeEventKey, e.GetType().Name);
            }
            else
            {
                logger.LogError("Activity logging failed for {EventKey}. Failure type: {FailureType}. Trace identifier: {TraceIdentifier}",
                    safeEventKey, e.GetType().Name, request.TraceIdentifier);
            }
        }
    }
}
