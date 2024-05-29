using Moonglade.Data.Entities;

namespace Moonglade.Pingback;

public enum PingbackStatus
{
    Success,
    GenericError,
    InvalidPingRequest,
    Error32TargetUriNotExist,
    Error48PingbackAlreadyRegistered,
    Error17SourceNotContainTargetUri,
    SpamDetectedFakeNotFound
}

public class PingbackResponse(PingbackStatus status)
{
    public PingbackStatus Status { get; set; } = status;

    public MentionEntity MentionEntity { get; set; }

    public static PingbackResponse GenericError => new(PingbackStatus.GenericError);
    public static PingbackResponse InvalidPingRequest => new(PingbackStatus.InvalidPingRequest);
    public static PingbackResponse Error32TargetUriNotExist => new(PingbackStatus.Error32TargetUriNotExist);
    public static PingbackResponse Error48PingbackAlreadyRegistered => new(PingbackStatus.Error48PingbackAlreadyRegistered);
    public static PingbackResponse Error17SourceNotContainTargetUri => new(PingbackStatus.Error17SourceNotContainTargetUri);
    public static PingbackResponse SpamDetectedFakeNotFound => new(PingbackStatus.SpamDetectedFakeNotFound);
}