using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moonglade.Utils;

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
        Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus status);
        Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(PostStatus status, int offset, int pageSize, string keyword = null);
        Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType);
        Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? catId = null);
        Task<IReadOnlyList<PostDigest>> ListArchive(int year, int? month);
        Task<IReadOnlyList<PostDigest>> ListByTag(int tagId, int pageSize, int pageIndex);
        Task<IReadOnlyList<PostDigest>> ListFeatured(int pageSize, int pageIndex);
        Task<IReadOnlyList<Archive>> GetArchiveAsync();
    }

    public class PostQueryService : IPostQueryService
    {
        private readonly ILogger<PostQueryService> _logger;
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
            IsOriginal = p.IsOriginal,
            OriginLink = p.OriginLink,
            HeroImageUrl = p.HeroImageUrl,
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

        private readonly Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>> _archiveSelector =
            p => new(p.Key.Year, p.Key.Month, p.Count());

        #endregion

        public PostQueryService(
            ILogger<PostQueryService> logger,
            IOptions<AppSettings> settings,
            IRepository<PostEntity> postRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo,
            IBlogCache cache)
        {
            _logger = logger;
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

            // Try to find by checksum
            var slugCheckSum = Helper.ComputeCheckSum($"{slug.Slug}#{date:yyyyMMdd}");
            ISpecification<PostEntity> spec = new PostSpec(slugCheckSum);

            var pid = await _postRepo.SelectFirstOrDefaultAsync(spec, p => p.Id);
            if (pid == Guid.Empty)
            {
                // Post does not have a checksum, fall back to old method
                spec = new PostSpec(date, slug.Slug);
                pid = await _postRepo.SelectFirstOrDefaultAsync(spec, x => x.Id);

                if (pid == Guid.Empty) return null;

                // Post is found, fill it's checksum so that next time the query can be run against checksum
                var p = await _postRepo.GetAsync(pid);
                p.HashCheckSum = slugCheckSum;

                await _postRepo.UpdateAsync(p);
            }

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

        public Task<IReadOnlyList<PostSegment>> ListSegment(PostStatus status)
        {
            var spec = new PostSpec(status);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public async Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegment(
            PostStatus status, int offset, int pageSize, string keyword = null)
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

            var spec = new PostPagingSpec(status, keyword, pageSize, offset);
            var posts = await _postRepo.SelectAsync(spec, _postSegmentSelector);

            Expression<Func<PostEntity, bool>> countExp = p => null == keyword || p.Title.Contains(keyword);

            switch (status)
            {
                case PostStatus.Draft:
                    countExp.AndAlso(p => !p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Published:
                    countExp.AndAlso(p => p.IsPublished && !p.IsDeleted);
                    break;
                case PostStatus.Deleted:
                    countExp.AndAlso(p => p.IsDeleted);
                    break;
                case PostStatus.NotSet:
                    countExp.AndAlso(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            var totalRows = _postRepo.Count(countExp);
            return (posts, totalRows);
        }

        public Task<IReadOnlyList<PostSegment>> ListInsights(PostInsightsType insightsType)
        {
            var spec = new PostInsightsSpec(insightsType, 10);
            return _postRepo.SelectAsync(spec, _postSegmentSelector);
        }

        public Task<IReadOnlyList<PostDigest>> List(int pageSize, int pageIndex, Guid? catId = null)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var spec = new PostPagingSpec(pageSize, pageIndex, catId);
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

        public async Task<IReadOnlyList<Archive>> GetArchiveAsync()
        {
            if (!_postRepo.Any(p => p.IsPublished && !p.IsDeleted))
            {
                return new List<Archive>();
            }

            var spec = new PostSpec(PostStatus.Published);
            var list = await _postRepo.SelectAsync(
                post => new(post.PubDateUtc.Value.Year, post.PubDateUtc.Value.Month),
                _archiveSelector, spec);

            return list;
        }

        public Task<IReadOnlyList<PostDigest>> ListArchive(int year, int? month)
        {
            if (year < DateTime.MinValue.Year || year > DateTime.MaxValue.Year)
            {
                _logger.LogError($"parameter '{nameof(year)}:{year}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(year));
            }

            if (month is > 12 or < 0)
            {
                _logger.LogError($"parameter '{nameof(month)}:{month}' is out of range");
                throw new ArgumentOutOfRangeException(nameof(month));
            }

            var spec = new PostSpec(year, month.GetValueOrDefault());
            var list = _postRepo.SelectAsync(spec, SharedSelectors.PostDigestSelector);
            return list;
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