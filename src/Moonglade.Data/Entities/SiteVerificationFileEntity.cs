namespace Moonglade.Data.Entities;

public class SiteVerificationFileEntity
{
    public Guid Id { get; set; }

    public string FileName { get; set; }

    public string NormalizedFileName { get; set; }

    public string Content { get; set; }

    public string ContentType { get; set; }

    public bool IsEnabled { get; set; }

    public DateTime CreatedTimeUtc { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}
