namespace Moonglade.Data.Specifications;

public record TagWithCount
{
    public string DisplayName { get; init; }
    public string NormalizedName { get; init; }
    public int PostCount { get; init; }
}