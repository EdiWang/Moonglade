using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;

namespace Moonglade.Web.Controllers;

/// <summary>
/// Base controller that provides common functionality for blog API controllers
/// </summary>
[Authorize(Policy = BlogAuthSchemas.AdministratorPolicy)]
[ApiController]
public abstract class BlogControllerBase(ICommandMediator commandMediator) : ControllerBase
{
    protected readonly ICommandMediator CommandMediator = commandMediator;

    protected async Task LogActivityAfterSaveAsync(
        ILogger logger,
        EventType eventType,
        string operation,
        string targetName,
        object metadata = null)
    {
        try
        {
            await LogActivityAsync(eventType, operation, targetName, metadata);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Changes saved, but activity logging failed for {EventType} ({Operation}). Trace identifier: {TraceIdentifier}",
                eventType, operation, HttpContext.TraceIdentifier);
        }
    }

    /// <summary>
    /// Records an activity log entry
    /// </summary>
    /// <param name="eventType">The type of event</param>
    /// <param name="operation">Description of the operation</param>
    /// <param name="targetName">Name of the target entity</param>
    /// <param name="metaData">Additional metadata</param>
    /// <param name="username">Optional username to log. If null, uses User.Identity.Name</param>
    protected async Task LogActivityAsync(
        EventType eventType,
        string operation,
        string targetName,
        object metaData = null,
        string username = null)
    {
        await CommandMediator.SendAsync(new CreateActivityLogCommand(
            eventType,
            username ?? User.Identity?.Name,
            operation,
            targetName,
            metaData,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString()
        ));
    }
}
