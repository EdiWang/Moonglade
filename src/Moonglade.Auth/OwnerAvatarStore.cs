#nullable enable

using Microsoft.EntityFrameworkCore;
using Moonglade.Data;
using Moonglade.Data.Entities;
using SkiaSharp;
using System.ComponentModel.DataAnnotations;

namespace Moonglade.Auth;

public sealed class OwnerAvatarStore(BlogDbContext db)
{
    public Task<bool> SetPresentationAsync(OwnerPresentation profile, CancellationToken ct)
    {
        Validator.ValidateObject(profile, new ValidationContext(profile), validateAllProperties: true);
        return SaveAsync(row =>
        {
            row.Name = profile.Name;
            row.Description = profile.Description;
        }, ct);
    }

    public Task<bool> UsesGravatarAsync(CancellationToken ct) =>
        db.OwnerAvatar.AnyAsync(avatar => avatar.UseGravatar, ct);

    public Task<string?> ReadImageAsync(CancellationToken ct) =>
        db.OwnerAvatar.Select(avatar => avatar.ImageBase64).SingleOrDefaultAsync(ct);

    public Task<bool> SetGravatarAsync(bool enabled, CancellationToken ct) =>
        SaveAsync(avatar => avatar.UseGravatar = enabled, ct);

    public Task SetImageAsync(string imageBase64, CancellationToken ct)
    {
        imageBase64 = imageBase64.Trim();
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(imageBase64);
        }
        catch (FormatException)
        {
            throw new ValidationException("Bad base64 data");
        }

        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            throw new ValidationException("Invalid image data.");
        if (codec.Info.Width != 300 || codec.Info.Height != 300)
            throw new ValidationException("Image size must be 300x300.");

        using var bitmap = new SKBitmap(codec.Info);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success)
            throw new ValidationException("Invalid image data.");

        return SaveAsync(avatar => avatar.ImageBase64 = imageBase64, ct);
    }

    private async Task<bool> SaveAsync(Action<OwnerAvatarEntity> update, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new InvalidOperationException("Owner avatar writes must own their commit.");

        var avatar = await db.OwnerAvatar.AsTracking().SingleOrDefaultAsync(ct);
        if (avatar is null)
        {
            try
            {
                return await SaveAsync(db.OwnerAvatar.Add(new OwnerAvatarEntity()).Entity, update, ct);
            }
            catch (DbUpdateException)
            {
                // A concurrent first write may have created the singleton row.
                avatar = await db.OwnerAvatar.AsTracking().SingleOrDefaultAsync(ct);
                if (avatar is null) throw;
            }
        }

        return await SaveAsync(avatar, update, ct);
    }

    private async Task<bool> SaveAsync(OwnerAvatarEntity avatar, Action<OwnerAvatarEntity> update, CancellationToken ct)
    {
        try
        {
            update(avatar);
            return await db.SaveChangesAsync(ct) > 0;
        }
        finally
        {
            db.Entry(avatar).State = EntityState.Detached;
        }
    }
}
