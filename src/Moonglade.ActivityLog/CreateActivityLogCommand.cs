using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;
using Moonglade.Data.Entities;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Moonglade.ActivityLog;

public record CreateActivityLogCommand(
    EventType EventType,
    string ActorId,
    string Operation,
    string TargetName,
    object MetaData = null,
    string IpAddress = null,
    string UserAgent = null) : ICommand;

public class CreateActivityLogCommandHandler(
    IRepositoryBase<ActivityLogEntity> repository,
    ILogger<CreateActivityLogCommandHandler> logger) : ICommandHandler<CreateActivityLogCommand>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    public async Task HandleAsync(CreateActivityLogCommand request, CancellationToken ct)
    {
        try
        {
            var entity = new ActivityLogEntity
            {
                EventId = (int)request.EventType,
                EventTimeUtc = DateTime.UtcNow,
                ActorId = TruncateString(request.ActorId, 100),
                Operation = TruncateString(request.Operation, 100),
                TargetName = TruncateString(request.TargetName, 200),
                MetaData = request.MetaData != null ? JsonSerializer.Serialize(request.MetaData, JsonOptions) : null,
                IpAddress = TruncateString(request.IpAddress, 50),
                UserAgent = TruncateString(request.UserAgent, 512)
            };

            await repository.AddAsync(entity, ct);

            logger.LogInformation("Activity log created: {Operation} on {TargetName} by {ActorId}",
                request.Operation, request.TargetName, request.ActorId);
        }
        catch (Exception e)
        {
            // Log the error but do not throw, as we don't want to block the main operation due to logging failure
            logger.LogError(e, "Failed to create activity log: {Operation} on {TargetName} by {ActorId}",
                request.Operation, request.TargetName, request.ActorId);
        }
    }
}
