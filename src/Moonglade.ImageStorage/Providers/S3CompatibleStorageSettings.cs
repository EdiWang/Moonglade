namespace Moonglade.ImageStorage.Providers;

public record S3CompatibleStorageSettings
{
    public string ServiceUrl { get; set; }

    public string Region { get; set; }

    public string AccessKeyId { get; set; }

    public string SecretAccessKey { get; set; }

    public string BucketName { get; set; } = "moonglade-images";

    public string SecondaryBucketName { get; set; } = "moonglade-images-origin";

    public bool ForcePathStyle { get; set; }
}
