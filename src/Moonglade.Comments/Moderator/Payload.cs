namespace Moonglade.Comments.Moderator;

public class Payload
{
    public string OriginAspNetRequestId { get; set; }

    public Content[] Contents { get; set; }
}

public class Content
{
    public string Id { get; set; }

    public string RawText { get; set; }
}