#nullable enable

using Microsoft.Extensions.Options;

namespace Moonglade.Web.Services;

public interface ICommentSubmissionGuard
{
    CommentSubmissionGuardResult Validate(CommentRequest? request);
}

public record CommentSubmissionGuardResult(bool Succeeded, string? ModelStateKey = null, string? ErrorMessage = null)
{
    public static CommentSubmissionGuardResult Success { get; } = new(true);

    public static CommentSubmissionGuardResult Failure(string modelStateKey, string errorMessage) =>
        new(false, modelStateKey, errorMessage);
}

public class CommentSubmissionGuard(
    IOptionsMonitor<CommentSubmissionGuardOptions> options,
    TimeProvider timeProvider) : ICommentSubmissionGuard
{
    private const string InvalidSubmissionMessage = "Invalid comment submission.";

    public CommentSubmissionGuardResult Validate(CommentRequest? request)
    {
        var settings = options.CurrentValue;
        if (!settings.Enabled || request is null)
        {
            return CommentSubmissionGuardResult.Success;
        }

        if (settings.HoneypotEnabled && !string.IsNullOrWhiteSpace(request.Source))
        {
            return CommentSubmissionGuardResult.Failure(nameof(CommentRequest.Source), InvalidSubmissionMessage);
        }

        if (settings.MinimumElapsedSeconds <= 0 && settings.MaxFormAgeMinutes <= 0)
        {
            return CommentSubmissionGuardResult.Success;
        }

        if (!request.FormRenderedUtc.HasValue)
        {
            return CommentSubmissionGuardResult.Failure(nameof(CommentRequest.FormRenderedUtc), InvalidSubmissionMessage);
        }

        DateTimeOffset formRenderedAt;
        try
        {
            formRenderedAt = DateTimeOffset.FromUnixTimeMilliseconds(request.FormRenderedUtc.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return CommentSubmissionGuardResult.Failure(nameof(CommentRequest.FormRenderedUtc), InvalidSubmissionMessage);
        }

        var elapsed = timeProvider.GetUtcNow() - formRenderedAt;
        if (settings.MinimumElapsedSeconds > 0 && elapsed < TimeSpan.FromSeconds(settings.MinimumElapsedSeconds))
        {
            return CommentSubmissionGuardResult.Failure(nameof(CommentRequest.FormRenderedUtc), InvalidSubmissionMessage);
        }

        if (settings.MaxFormAgeMinutes > 0 && elapsed > TimeSpan.FromMinutes(settings.MaxFormAgeMinutes))
        {
            return CommentSubmissionGuardResult.Failure(nameof(CommentRequest.FormRenderedUtc), InvalidSubmissionMessage);
        }

        return CommentSubmissionGuardResult.Success;
    }
}
