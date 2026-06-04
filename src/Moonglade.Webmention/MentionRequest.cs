namespace Moonglade.Webmention;

public record MentionRequest
{
    public string SourceUrl { get; set; } = string.Empty;

    public string TargetUrl { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public bool ContainsHtml { get; set; }

    public bool SourceHasTarget { get; set; }
}