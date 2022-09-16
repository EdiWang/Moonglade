namespace Moonglade.Core.PageFeature;

public record PageSegment
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreateTimeUtc { get; set; }
}