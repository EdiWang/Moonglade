namespace Moonglade.Webmention;

public interface IWebmentionRequestor
{
    Task<HttpResponseMessage> Send(Uri sourceUrl, Uri targetUrl, Uri url);
}

public class WebmentionRequestor : IWebmentionRequestor
{
    private readonly HttpClient _httpClient;

    public WebmentionRequestor(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-us");
    }

    public Task<HttpResponseMessage> Send(Uri sourceUrl, Uri targetUrl, Uri url)
    {
        var values = new Dictionary<string, string>
        {
            { "source", sourceUrl.ToString() },
            { "target", targetUrl.ToString() }
        };

        var content = new FormUrlEncodedContent(values);
        return _httpClient.PostAsync(url, content);
    }
}