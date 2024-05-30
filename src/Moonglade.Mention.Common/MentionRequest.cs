namespace Moonglade.Mention.Common;

public record MentionRequest
{
    public string SourceUrl { get; set; }

    public string TargetUrl { get; set; }

    public string Title { get; set; }

    public bool ContainsHtml { get; set; }

    public bool SourceHasTarget { get; set; }
}