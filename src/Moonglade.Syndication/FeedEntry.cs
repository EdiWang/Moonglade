﻿namespace Moonglade.Syndication;

public record FeedEntry
{
    public string Id { get; set; }
    public DateTime PubDateUtc { get; set; }
    public string Title { get; set; }
    public string Link { get; set; }
    public string Description { get; set; }
    public string Author { get; set; }
    public string AuthorEmail { get; set; }
    public string LangCode { get; set; }
    public string[] Categories { get; set; }
}