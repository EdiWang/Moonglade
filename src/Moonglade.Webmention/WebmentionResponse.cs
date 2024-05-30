using Moonglade.Data.Entities;

namespace Moonglade.Webmention;

public class WebmentionResponse
{
    public WebmentionStatus Status { get; set; }
    public MentionEntity MentionEntity { get; set; }
}

public enum WebmentionStatus
{
    Success
}