namespace Moonglade.Data.DTO;

public record PostCommandResult
{
    public Guid Id { get; init; }
    public string RouteLink { get; init; }
    public string PostContent { get; init; }
    public DateTime? PubDateUtc { get; init; }
    public DateTime? LastModifiedUtc { get; init; }
}
