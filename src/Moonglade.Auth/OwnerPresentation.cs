using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public sealed record OwnerPresentation
{
    [Required, StringLength(32)]
    public string Name { get; init; }

    [Required, StringLength(256)]
    public string Description { get; init; }

    public static OwnerPresentation Default { get; } = new() { Name = "Admin", Description = "Moonglade Admin" };

    internal static async Task<OwnerPresentation> ReadAsync(BlogDbContext db, CancellationToken ct) =>
        await db.OwnerAvatar.Select(profile => new OwnerPresentation
        {
            Name = profile.Name ?? Default.Name,
            Description = profile.Description ?? Default.Description
        })
            .SingleOrDefaultAsync(ct) ?? Default;
}
