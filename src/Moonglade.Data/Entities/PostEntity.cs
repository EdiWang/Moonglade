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
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsOriginal { get; set; }
    public bool IsOutdated { get; set; }
    public string OriginLink { get; set; }
    public string HeroImageUrl { get; set; }
    public string InlineCss { get; set; }
    public bool IsFeatured { get; set; }
    public int HashCheckSum { get; set; }

    public virtual ICollection<CommentEntity> Comments { get; set; }
    public virtual ICollection<PostCategoryEntity> PostCategory { get; set; }
    public virtual ICollection<TagEntity> Tags { get; set; }
}