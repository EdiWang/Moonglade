using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;

namespace Moonglade.Core
{
    public interface IPostQueryService
    {
        int CountPublic();
        int CountByCategory(Guid catId);
        int CountByTag(int tagId);
        int CountByFeatured();
        Task<Post> GetAsync(Guid id);
        Task<Post> GetAsync(PostSlug slug);
        Task<Post> GetDraft(Guid postId);
        Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus postStatus);
        Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(PostStatus postStatus, int offset, int pageSize, string keyword = null);
        Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType);
        Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? categoryId = null);
        Task<IReadOnlyList<PostDigest>> ListByTag(int tagId, int pageSize, int pageIndex);
        Task<IReadOnlyList<PostDigest>> ListFeatured(int pageSize, int pageIndex);
    }

    public class PostQueryService : IPostQueryService
    {
        private readonly IBlogCache _cache;
        private readonly AppSettings _settings;

        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        #endregion

        #region Selectors

        private readonly Expression<Func<PostEntity, Post>> _postSelector = p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            RawPostContent = p.PostContent,
            ContentAbstract = p.ContentAbstract,
            CommentEnabled = p.CommentEnabled,
            CreateTimeUtc = p.CreateTimeUtc,
            PubDateUtc = p.PubDateUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            IsPublished = p.IsPublished,
            ExposedToSiteMap = p.ExposedToSiteMap,
            IsFeedIncluded = p.IsFeedIncluded,
            Featured = p.IsFeatured,
            ContentLanguageCode = p.ContentLanguageCode,
            Tags = p.Tags.Select(pt => new Tag
            {
                Id = pt.Id,
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            }).ToArray(),
            Categories = p.PostCategory.Select(pc => new Category
            {
                Id = pc.CategoryId,
                DisplayName = pc.Category.DisplayName,
                RouteName = pc.Category.RouteName,
                Note = pc.Category.Note
            }).ToArray()
        };

        private readonly Expression<Func<PostEntity, PostSegment>> _postSegmentSelector = p => new()
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            PubDateUtc = p.PubDateUtc,
            IsPublished = p.IsPublished,
            IsDeleted = p.IsDeleted,
            CreateTimeUtc = p.CreateTimeUtc,
            LastModifiedUtc = p.LastModifiedUtc,
            ContentAbstract = p.ContentAbstract,
            Hits = p.PostExtension.Hits
        };

        private readonly Expression<Func<PostTagEntity, PostDigest>> _postDigestSelectorByTag = p => new()
        {
            Title = p.Post.Title,
            Slug = p.Post.Slug,
            ContentAbstract = p.Post.ContentAbstract,
            PubDateUtc = p.Post.PubDateUtc.GetValueOrDefault(),
            LangCode = p.Post.ContentLanguageCode,
            IsFeatured = p.Post.IsFeatured,
            Tags = p.Post.Tags.Select(pt => new Tag
            {
                NormalizedName = pt.NormalizedName,
                DisplayName = pt.DisplayName
            })
        };

        #endregion

        public PostQueryService(
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IBlogCache cache)
        {
            _settings = settings.Value;
            _postRepo = postRepo;
            _postTagRepo = postTagRepo;
            _postCatRepo = postCatRepo;
            _cache = cache;
        }

        #region Counts

        public int CountPublic() => _postRepo.Count(p => p.IsPublished && !p.IsDeleted);

        public int CountByCategory(Guid catId) => _postCatRepo.Count(c => c.CategoryId == catId
                                                                          && c.Post.IsPublished
                                                                          && !c.Post.IsDeleted);
        public int CountByTag(int tagId) => _postTagRepo.Count(p => p.TagId == tagId && p.Post.IsPublished && !p.Post.IsDeleted);

        public int CountByFeatured() => _postRepo.Count(p => p.IsFeatured && p.IsPublished && !p.IsDeleted);

        #endregion

        public Task<Post> GetAsync(Guid id)
        {
            var spec = new PostSpec(id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
            return post;
        }

        public async Task<Post> GetAsync(PostSlug slug)
        {
            var date = new DateTime(slug.Year, slug.Month, slug.Day);
            var spec = new PostSpec(date, slug.Slug);

            var pid = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Id);
            if (pid == Guid.Empty) return null;

            var psm = await _cache.GetOrCreateAsync(CacheDivision.Post, $"{pid}", async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(_settings.CacheSlidingExpirationMinutes["Post"]);

                var post = await _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
                return post;
            });

            return psm;
        }

        public Task<Post> GetDraft(Guid id)
        {
            var spec = new PostSpec(id);
            var post = _postRepo.SelectFirstOrDefaultAsync(spec, _postSelector);
            return post;
        }

        public Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus postStatus)
        {
            var spec = new PostSpec(postStatus);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public async Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(
            PostStatus postStatus, int offset, int pageSize, string keyword = null)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    $"{nameof(pageSize)} can not be less than 1, current value: {pageSize}.");
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset),
                    $"{nameof(offset)} can not be less than 0, current value: {offset}.");
            }

            var spec = new PostPagingSpec(postStatus, keyword, pageSize, offset);
            var posts = await _postRepo.SelectAsync(spec, _postSegmentSelector);

            Expression<Func<PostEntity, bool>> countExpression = p => null == keyword || p.Title.Contains(keyword);

            switch (postStatus)
            {
                case PostStatus.Draft:
                    countExpression.AndAlso(p => !p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Published:
                    countExpression.AndAlso(p => p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Deleted:
                    countExpression.AndAlso(p => p.IsDeleted);
                    break;
                case PostStatus.NotSet:
                    countExpression.AndAlso(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(postStatus), postStatus, null);
            }

            var totalRows = _postRepo.Count(countExpression);
            return (posts, totalRows);
        }

        public Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType)
        {
            var spec = new PostInsightsSpec(insightsType, 10);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? categoryId = null)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var spec = new PostPagingSpec(pageSize, pageIndex, categoryId);
            return _postRepo.SelectAsync(spec, SharedSelectors.PostDigestSelector);
        }

        public Task<IReadOnlyList<PostDigest>> ListByTag(int tagId, int pageSize, int pageIndex)
        {
            if (tagId <= 0) throw new ArgumentOutOfRangeException(nameof(tagId));
            ValidatePagingParameters(pageSize, pageIndex);

            var posts = _postTagRepo.SelectAsync(new PostTagSpec(tagId, pageSize, pageIndex), _postDigestSelectorByTag);
            return posts;
        }

        public Task<IReadOnlyList<PostDigest>> ListFeatured(int pageSize, int pageIndex)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var posts = _postRepo.SelectAsync(new FeaturedPostSpec(pageSize, pageIndex), SharedSelectors.PostDigestSelector);
            return posts;
        }

        private static void ValidatePagingParameters(int pageSize, int pageIndex)
        {
            if (pageSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    $"{nameof(pageSize)} can not be less than 1, current value: {pageSize}.");
            }

            if (pageIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex),
                    $"{nameof(pageIndex)} can not be less than 1, current value: {pageIndex}.");
            }
        }
    }
}