namespace Moonglade.Data.DTO;

public record TagCommandResult
{
    public int Id { get; init; }
    public string DisplayName { get; init; }
    public string NormalizedName { get; init; }
}
