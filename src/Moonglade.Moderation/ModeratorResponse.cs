namespace Moonglade.Comments.Moderator;

public class ModeratorResponse
{
    public string OriginAspNetRequestId { get; set; }
    public string Moderator { get; set; }
    public string Mode { get; set; }
    public ProcessedContent[] ProcessedContents { get; set; }
    public bool? Positive { get; set; }
}

public class ProcessedContent
{
    public string Id { get; set; }

    public string ProcessedText { get; set; }
}