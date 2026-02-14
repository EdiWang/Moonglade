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

    public async Task HandleAsync(CreateActivityLogCommand request, CancellationToken ct)
    {
        var entity = new ActivityLogEntity
        {
            EventId = (int)request.EventType,
            EventTimeUtc = DateTime.UtcNow,
            ActorId = request.ActorId?.Trim(),
            Operation = request.Operation?.Trim(),
            TargetName = request.TargetName?.Trim(),
            MetaData = request.MetaData != null ? JsonSerializer.Serialize(request.MetaData, JsonOptions) : null,
            IpAddress = request.IpAddress?.Trim(),
            UserAgent = request.UserAgent?.Trim()
        };

        await repository.AddAsync(entity, ct);

        logger.LogInformation("Activity log created: {Operation} on {TargetName} by {ActorId}",
            request.Operation, request.TargetName, request.ActorId);
    }
}
