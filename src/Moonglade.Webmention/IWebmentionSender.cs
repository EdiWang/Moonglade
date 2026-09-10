namespace Moonglade.Webmention;

public interface IWebmentionSender
{
    Task SendWebmentionAsync(string postUrl, string postContent);
}
