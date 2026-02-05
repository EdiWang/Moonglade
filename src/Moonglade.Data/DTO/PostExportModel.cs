namespace Moonglade.Data.DTO;

public class PostExportModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string RouteLink { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string ContentAbstract { get; set; }
    public string PostContent { get; set; } = null!;
    public DateTime CreateTimeUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public DateTime? ScheduledPublishTimeUtc { get; set; }
    public bool CommentEnabled { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public string ContentLanguageCode { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public bool IsFeedIncluded { get; set; }
    public bool IsFeatured { get; set; }
    public PostStatus PostStatus { get; set; }
    public bool IsOutdated { get; set; }
    public string Keywords { get; set; }
    public List<string> Categories { get; set; } = [];
    public List<string> Tags { get; set; } = [];
}
