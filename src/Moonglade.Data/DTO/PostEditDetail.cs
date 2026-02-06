namespace Moonglade.Data.DTO;

public record PostEditDetail
{
    public Guid PostId { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string EditorContent { get; set; }
    public PostStatus PostStatus { get; set; }
    public bool EnableComment { get; set; }
    public bool FeedIncluded { get; set; }
    public bool Featured { get; set; }
    public bool IsOutdated { get; set; }
    public string LanguageCode { get; set; }
    public string ContentAbstract { get; set; }
    public string Keywords { get; set; }
    public string Tags { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ScheduledPublishTimeUtc { get; set; }
    public string LastModifiedUtc { get; set; }
    public Guid[] SelectedCatIds { get; set; }
}
