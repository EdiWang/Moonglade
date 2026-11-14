#nullable enable

using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Data.Entities;
using Moonglade.Web.Services;

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
        ActivityLogDispatcher.SendAsync(
            CommandMediator,
            logger,
            eventKey,
            () => new CreateActivityLogCommand(
                eventKey,
                ActivityActorKind.Administrator,
                User.Identity?.Name,
                targetId,
                targetDisplayName,
                details,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            ActivityLogDispatchSource.Controller,
            HttpContext.TraceIdentifier);
}
