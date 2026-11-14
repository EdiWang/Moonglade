#nullable enable

namespace Moonglade.Data.Entities;

public sealed class OwnerAvatarEntity
{
    public const int OwnerSlot = 1;

    public int Id { get; init; } = OwnerSlot;
    [System.ComponentModel.DataAnnotations.MaxLength(32)]
    public string? Name { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(256)]
    public string? Description { get; set; }

    public bool UseGravatar { get; set; }
    public string? ImageBase64 { get; set; }
}
