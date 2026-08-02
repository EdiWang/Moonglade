namespace Moonglade.Email.Core;

public record EmailNotification
{
    public string DistributionList { get; init; }
    public string MessageType { get; init; }
    public string MessageBody { get; init; }
}
