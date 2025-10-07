using Moonglade.Data.DTO;
using Moonglade.Data.Entities;

namespace Moonglade.Data.Specifications;

public class PostEntityToPostExportModelSpec : Specification<PostEntity, PostExportModel>
{
    public PostEntityToPostExportModelSpec()
    {
        Query.Select(p => new PostExportModel
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            RouteLink = p.RouteLink,
            Author = p.Author,
            ContentAbstract = p.ContentAbstract,
            PostContent = p.PostContent,
            HeroImageUrl = p.HeroImageUrl,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ScheduledPublishTimeUtc = p.ScheduledPublishTimeUtc,
            CommentEnabled = p.CommentEnabled,
            PubDateUtc = p.PubDateUtc,
            ContentLanguageCode = p.ContentLanguageCode,
            IsDeleted = p.IsDeleted,
            IsFeedIncluded = p.IsFeedIncluded,
            IsFeatured = p.IsFeatured,
            PostStatus = p.PostStatus,
            IsOutdated = p.IsOutdated,
            Keywords = p.Keywords,
            Categories = p.PostCategory.Select(pc => pc.Category.DisplayName).ToList(),
            Tags = p.Tags.Select(t => t.DisplayName).ToList()
        });
    }
}