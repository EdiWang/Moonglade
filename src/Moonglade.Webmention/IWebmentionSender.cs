namespace Moonglade.Webmention;

public interface IWebmentionSender
{
    Task SendWebmentionAsync(string postUrl, string postContent);
}

public class WebmentionSendResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string ResponseContent { get; set; }
    public string AdditionalInfo { get; set; }
}