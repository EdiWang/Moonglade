using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public class WebmentionResponse(WebmentionStatus Status)
{
    public WebmentionStatus Status { get; set; }
    public MentionEntity MentionEntity { get; set; }

    public static WebmentionResponse GenericError => new(WebmentionStatus.GenericError);
    public static WebmentionResponse InvalidWebmentionRequest => new(WebmentionStatus.InvalidWebmentionRequest);
    public static WebmentionResponse ErrorTargetUriNotExist => new(WebmentionStatus.ErrorTargetUriNotExist);
    public static WebmentionResponse ErrorWebmentionAlreadyRegistered => new(WebmentionStatus.ErrorWebmentionAlreadyRegistered);
    public static WebmentionResponse ErrorSourceNotContainTargetUri => new(WebmentionStatus.ErrorSourceNotContainTargetUri);
    public static WebmentionResponse SpamDetectedFakeNotFound => new(WebmentionStatus.SpamDetectedFakeNotFound);
}

public enum WebmentionStatus
{
    Success,
    GenericError,
    InvalidWebmentionRequest,
    ErrorTargetUriNotExist,
    ErrorWebmentionAlreadyRegistered,
    ErrorSourceNotContainTargetUri,
    SpamDetectedFakeNotFound
}