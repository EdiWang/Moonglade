namespace Moonglade.Data.DTO;

public record SiteMapInfo
{
    public string Slug { get; init; }
    public DateTime CreateTimeUtc { get; init; }
    public DateTime? UpdateTimeUtc { get; init; }
}