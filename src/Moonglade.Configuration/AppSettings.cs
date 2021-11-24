namespace Moonglade.Configuration;

public class AppSettings
{
    public EditorChoice Editor { get; set; }
    public IDictionary<string, int> CacheSlidingExpirationMinutes { get; set; }
}