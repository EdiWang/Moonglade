﻿namespace Moonglade.Data.DTO;

public class PostDigest
{
    public DateTime PubDateUtc { get; set; }

    public string Title { get; set; }

    public string Slug { get; set; }

    public string ContentAbstract { get; set; }

    public string LangCode { get; set; }

    public bool IsFeatured { get; set; }

    public IEnumerable<Tag> Tags { get; set; }
}