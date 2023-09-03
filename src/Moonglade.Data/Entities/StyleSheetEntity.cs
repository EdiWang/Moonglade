namespace Moonglade.Data.Entities;

public class StyleSheetEntity
{
    public Guid Id { get; set; }

    public string FriendlyName { get; set; }

    public string Hash { get; set; }

    public string CssContent { get; set; }

    public DateTime LastModifiedTimeUtc { get; set; }
}