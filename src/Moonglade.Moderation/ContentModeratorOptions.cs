namespace Moonglade.Moderation;

public class ContentModeratorOptions
{
    public string Provider { get; set; } = "local";
    public string LocalKeywords { get; set; } = string.Empty;
    public string ApiEndpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
