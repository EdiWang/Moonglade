namespace Moonglade.Webmention;

public interface IWebmentionSender
{
    Task<WebmentionSendResult> SendWebmentionAsync(string sourceUrl, string targetUrl);
}

public class WebmentionSendResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string ResponseContent { get; set; }
    public string AdditionalInfo { get; set; }
}