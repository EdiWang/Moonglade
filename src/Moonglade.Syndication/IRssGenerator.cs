namespace Moonglade.Syndication;

public interface IRssGenerator
{
    Task<string> WriteRssAsync();
}