namespace Moonglade.Data.Entities;

public class PostEntity
{
    public PostEntity()
    {
        Comments = new HashSet<CommentEntity>();
        PostCategory = new HashSet<PostCategoryEntity>();
        Tags = new HashSet<TagEntity>();
    }

    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; }
    public string Author { get; set; }
    public string PostContent { get; set; }
    public bool CommentEnabled { get; set; }
    public DateTime CreateTimeUtc { get; set; }
    public string ContentAbstract { get; set; }
    public string ContentLanguageCode { get; set; }
    public bool IsFeedIncluded { get; set; }
    public DateTime? PubDateUtc { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public DateTime? ScheduledPublishTimeUtc { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOutdated { get; set; }
    public bool IsFeatured { get; set; }
    public string RouteLink { get; set; }
    public PostStatus PostStatus { get; set; }
    public string Keywords { get; set; }

    public ICollection<CommentEntity> Comments { get; set; }
    public ICollection<PostCategoryEntity> PostCategory { get; set; }
    public ICollection<TagEntity> Tags { get; set; }
}