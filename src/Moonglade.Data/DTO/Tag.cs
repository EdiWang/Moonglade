namespace Moonglade.Data.DTO;

public record Tag
{
    public string DisplayName { get; init; }

    public string NormalizedName { get; init; }
}