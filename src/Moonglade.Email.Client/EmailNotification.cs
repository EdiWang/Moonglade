namespace Moonglade.Email.Client;

internal record EmailNotification
{
    public string Type { get; set; }
    public string[] Receipts { get; set; }
    public object Payload { get; set; }
    public string OriginAspNetRequestId { get; set; } = Guid.Empty.ToString();
}