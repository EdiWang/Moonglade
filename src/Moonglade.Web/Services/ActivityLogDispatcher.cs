#nullable enable

using LiteBus.Commands.Abstractions;
using Moonglade.ActivityLog;

namespace Moonglade.Web.Services;

internal enum ActivityLogDispatchSource
{
    Controller,
    PostSave,
    ScheduledPublication
}

internal static class ActivityLogDispatcher
{
    public static async Task SendAsync(
        ICommandMediator commandMediator,
        ILogger logger,
        string eventKey,
        Func<CreateActivityLogCommand> createCommand,
        ActivityLogDispatchSource source,
        string? traceIdentifier = null,
        CancellationToken ct = default)
    {
        try
        {
            await commandMediator.SendAsync(createCommand() with { TraceIdentifier = traceIdentifier }, ct);
        }
        catch (Exception exception)
        {
            var safeEventKey = ActivityEvents.IsSupported(eventKey) ? eventKey : "(unsupported)";
            switch (source)
            {
                case ActivityLogDispatchSource.Controller:
                    logger.LogError(
                        "Changes saved, but activity logging failed for {EventKey}. Failure type: {FailureType}. Trace identifier: {TraceIdentifier}",
                        safeEventKey, exception.GetType().Name, traceIdentifier);
                    break;
                case ActivityLogDispatchSource.PostSave:
                    if (string.IsNullOrEmpty(traceIdentifier))
                    {
                        logger.LogError(
                            "Post was saved, but activity logging failed for event {EventKey}. Failure type: {FailureType}.",
                            safeEventKey, exception.GetType().Name);
                    }
                    else
                    {
                        logger.LogError(
                            "Post was saved, but activity logging failed for event {EventKey}. Failure type: {FailureType}. Trace identifier: {TraceIdentifier}",
                            safeEventKey, exception.GetType().Name, traceIdentifier);
                    }
                    break;
                case ActivityLogDispatchSource.ScheduledPublication:
                    logger.LogError(
                        "Scheduled publication activity logging failed for {EventKey}. Failure type: {FailureType}",
                        safeEventKey, exception.GetType().Name);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(source));
            }
        }
    }
}
