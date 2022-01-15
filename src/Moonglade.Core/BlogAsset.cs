namespace Moonglade.Core;

internal class BlogAsset
{
    public Guid Id { get; set; }

    public string Base64Data { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}

public class AssetId
{
    public static Guid SiteIconBase64 = Guid.Parse("aec91802-6867-4618-86bd-0620213802a8");
    public static Guid AvatarBase64 = Guid.Parse("0922e4dc-b47b-44e2-a493-05d4e624381c");
}