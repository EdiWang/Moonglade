#nullable enable

using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;

namespace Moonglade.Web.Controllers;

[Authorize(Policy = BlogAuthSchemas.AdministratorPolicy)]
[ApiController]
public abstract class BlogControllerBase(ICommandMediator commandMediator, ILogger logger) : ControllerBase
{
    protected readonly ICommandMediator CommandMediator = commandMediator;

    protected Task LogActivityAsync(
        string eventKey,
        string? targetDisplayName,
        IReadOnlyDictionary<string, object?>? details = null,
        string? targetId = null) =>
        LogActivityAfterSaveAsync(logger, eventKey, targetDisplayName, details, targetId);

    protected async Task LogActivityAfterSaveAsync(
        ILogger activityLogger,
        string eventKey,
        string? targetDisplayName,
        IReadOnlyDictionary<string, object?>? details = null,
        string? targetId = null)
    {
        try
        {
            await CommandMediator.SendAsync(new CreateActivityLogCommand(
                eventKey,
                ActivityActorKind.Administrator,
                User.Identity?.Name,
                targetId,
                targetDisplayName,
                details,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()));
        }
        catch (Exception exception)
        {
            activityLogger.LogError(
                "Changes saved, but activity logging failed for {EventKey}. Failure type: {FailureType}. Trace identifier: {TraceIdentifier}",
                ActivityEvents.IsSupported(eventKey) ? eventKey : "(unsupported)", exception.GetType().Name, HttpContext.TraceIdentifier);
        }
    }
}
