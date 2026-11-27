#nullable enable

using LiteBus.Commands.Abstractions;
using Moonglade.Web.Services;

namespace Moonglade.Web.Controllers;

[Authorize(Policy = BlogAuthSchemas.AdministratorPolicy)]
public abstract class BlogControllerBase(ICommandMediator commandMediator, ILogger logger)
    : ActivityControllerBase(commandMediator, logger);

[Authorize(Policy = BlogAuthSchemas.ContentManagementPolicy)]
public abstract class ContentControllerBase(ICommandMediator commandMediator, ILogger logger)
    : ActivityControllerBase(commandMediator, logger);

[ApiController]
public abstract class ActivityControllerBase(ICommandMediator commandMediator, ILogger logger) : ControllerBase
{
    protected readonly ICommandMediator CommandMediator = commandMediator;

    protected Task LogActivityAsync(
        string eventKey,
        string? targetDisplayName,
        IReadOnlyDictionary<string, object?>? details = null,
        string? targetId = null) =>
        ActivityLogDispatcher.SendForRequestAsync(CommandMediator, logger, HttpContext, eventKey, targetDisplayName, details, targetId);
}
