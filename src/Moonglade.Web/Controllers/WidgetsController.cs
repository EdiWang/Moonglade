using LiteBus.Commands.Abstractions;
using LiteBus.Queries.Abstractions;
using Moonglade.ActivityLog;
using Moonglade.Widgets;

namespace Moonglade.Web.Controllers;

[Route("api/[controller]")]
public class WidgetsController(
    ICacheAside cache,
    IQueryMediator queryMediator,
    ICommandMediator commandMediator,
    ILogger<WidgetsController> logger) : BlogControllerBase(commandMediator, logger)
{
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Collection(CancellationToken ct)
    {
        var widgets = await queryMediator.QueryAsync(new ListWidgetsQuery(), ct);
        return Ok(new WidgetCollection(widgets.Select(WidgetSnapshot.From).ToArray()));
    }

    [HttpPut]
    public async Task<IActionResult> Save(WidgetCollectionRequest request, CancellationToken ct)
    {
        var saved = await CommandMediator.SendAsync(new SaveWidgetCollectionCommand(request), ct);
        var cacheFailed = false;
        try
        {
            cache.Remove(BlogCachePartition.General.ToString(), "widgets");
        }
        catch (Exception exception)
        {
            cacheFailed = true;
            logger.LogError("Widgets saved, but cache invalidation failed. Failure type: {FailureType}. Trace identifier: {TraceIdentifier}",
                exception.GetType().Name, HttpContext.TraceIdentifier);
        }

        foreach (var change in saved.Changes)
        {
            var eventKey = change.Kind switch
            {
                WidgetChangeKind.Created => ActivityEvents.WidgetCreated,
                WidgetChangeKind.Updated => ActivityEvents.WidgetUpdated,
                _ => ActivityEvents.WidgetDeleted
            };
            var details = change.Kind == WidgetChangeKind.Deleted
                ? ActivityLogDetails.Create(("WidgetId", change.Id))
                : ActivityLogDetails.Create(("WidgetId", change.Id), ("WidgetType", change.WidgetType));
            await LogActivityAsync(eventKey, change.Title, details, change.Id.ToString());
        }

        if (cacheFailed)
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Widgets were saved, but cache invalidation failed.",
                Detail = "Reload to compare the saved collection before retrying."
            };
            problem.Extensions["committed"] = true;
            problem.Extensions["savedCollection"] = saved.Collection;
            return StatusCode(problem.Status.Value, problem);
        }
        return Ok(saved.Collection);
    }
}
