namespace Moonglade.Pingback
{
    public enum PingbackServiceResponse
    {
        Success,
        GenericError,
        InvalidPingRequest,
        Error32TargetUriNotExist,
        Error48PingbackAlreadyRegistered,
        Error17SourceNotContainTargetUri,
        SpamDetectedFakeNotFound
    }

    public enum PingbackValidationResult
    {
        GenericError,
        ValidPingRequest,
        TerminatedMethodNotFound,
        TerminatedUrlNotFound
    }
}