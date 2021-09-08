using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Core
{
    public interface IPostQueryService
    {
        int CountPublic();
        int CountByCategory(Guid catId);
        int CountByTag(int tagId);
        int CountByFeatured();
        Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegmentAsync(PostStatus status, int offset, int pageSize, string keyword = null);
        Task<IReadOnlyList<PostDigest>> ListAsync(int pageSize, int pageIndex, Guid? catId = null);
    }

    public class PostQueryService : IPostQueryService
    {
        #region Repository Objects

        private readonly IRepository<PostEntity> _postRepo;
        private readonly IRepository<PostTagEntity> _postTagRepo;
        private readonly IRepository<PostCategoryEntity> _postCatRepo;

        #endregion

        public PostQueryService(
            IRepository<PostEntity> postRepo,
            IRepository<PostTagEntity> postTagRepo,
            IRepository<PostCategoryEntity> postCatRepo)
        {
            _postRepo = postRepo;
            _postTagRepo = postTagRepo;
            _postCatRepo = postCatRepo;
        }

        #region Counts

        public int CountPublic() => _postRepo.Count(p => p.IsPublished && !p.IsDeleted);

        public int CountByCategory(Guid catId) => _postCatRepo.Count(c => c.CategoryId == catId
                                                                          && c.Post.IsPublished
                                                                          && !c.Post.IsDeleted);
        public int CountByTag(int tagId) => _postTagRepo.Count(p => p.TagId == tagId && p.Post.IsPublished && !p.Post.IsDeleted);

        public int CountByFeatured() => _postRepo.Count(p => p.IsFeatured && p.IsPublished && !p.IsDeleted);

        #endregion

        public async Task<(IReadOnlyList<PostSegment> Posts, int TotalRows)> ListSegmentAsync(
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
            var posts = await _postRepo.SelectAsync(spec, PostSegment.EntitySelector);

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
                case PostStatus.Default:
                    countExp.AndAlso(p => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            var totalRows = _postRepo.Count(countExp);
            return (posts, totalRows);
        }

        public Task<IReadOnlyList<PostDigest>> ListAsync(int pageSize, int pageIndex, Guid? catId = null)
        {
            ValidatePagingParameters(pageSize, pageIndex);

            var spec = new PostPagingSpec(pageSize, pageIndex, catId);
            return _postRepo.SelectAsync(spec, PostDigest.EntitySelector);
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