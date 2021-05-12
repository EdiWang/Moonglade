using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class PostQueryServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PostQueryService>> _mockLogger;
        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<IRepository<PostTagEntity>> _mockPostTagEntityRepo;
        private Mock<IRepository<PostCategoryEntity>> _mockRepositoryPostCategoryEntity;
        private Mock<IBlogCache> _mockBlogCache;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<PostQueryService>>();
            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockPostTagEntityRepo = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockRepositoryPostCategoryEntity = _mockRepository.Create<IRepository<PostCategoryEntity>>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private PostQueryService CreateService()
        {
            return new(
                _mockLogger.Object,
                _mockOptionsAppSettings.Object,
                _mockPostEntityRepo.Object,
                _mockPostTagEntityRepo.Object,
                _mockRepositoryPostCategoryEntity.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task GetAsync_OK()
        {
            var svc = CreateService();
            var result = await svc.GetAsync(Uid);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>(), true));
        }

        [Test]
        public async Task GetAsync_Slug_OK()
        {
            _mockPostEntityRepo
                .Setup(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(),
                    It.IsAny<Expression<Func<PostEntity, Guid>>>(), true)).Returns(Task.FromResult(Uid));

            var svc = CreateService();
            var result = await svc.GetAsync(new PostSlug(996, 3, 5, "work-996-junk-35"));

            _mockBlogCache.Verify(p => p.GetOrCreateAsync(CacheDivision.Post, Uid.ToString(), It.IsAny<Func<ICacheEntry, Task<Post>>>()));
        }

        [Test]
        public async Task GetDraft_OK()
        {
            var svc = CreateService();
            var result = await svc.GetDraft(Uid);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>(), true));
        }

        [Test]
        public async Task ListSegment_OK()
        {
            var svc = CreateService();
            var result = await svc.ListSegment(PostStatus.Published);

            _mockPostEntityRepo.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>(), true));
        }

        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        public void ListSegment_InvalidParameter(int offset, int pageSize)
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListSegment(PostStatus.Published, offset, pageSize);
            });
        }

        [TestCase(PostStatus.Published)]
        [TestCase(PostStatus.Deleted)]
        [TestCase(PostStatus.Draft)]
        [TestCase(PostStatus.NotSet)]
        public async Task ListSegment_WithPaging(PostStatus postStatus)
        {
            IReadOnlyList<PostSegment> segments = new List<PostSegment>()
            {
                new()
                {
                    Id = Uid, ContentAbstract = "Work 996 and get into ICU", CreateTimeUtc = DateTime.MinValue, Hits = 996, Slug = "work-996", Title = "Fubao"
                }
            };

            _mockPostEntityRepo
                .Setup(p => p.SelectAsync(It.IsAny<PostPagingSpec>(),
                    It.IsAny<Expression<Func<PostEntity, PostSegment>>>(), true)).Returns(Task.FromResult(segments));

            _mockPostEntityRepo.Setup(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>())).Returns(996);

            var svc = CreateService();
            var result = await svc.ListSegment(postStatus, 0, 35);

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task ListInsights_OK()
        {
            var svc = CreateService();
            var result = await svc.ListInsights(PostInsightsType.TopRead);

            _mockPostEntityRepo.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostInsightsSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>(), true));
        }

        [Test]
        public async Task List_OK()
        {
            var svc = CreateService();
            await svc.List(35, 7, Uid);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostPagingSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>(), true));
        }

        [Test]
        public void List_InvalidPageSize()
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.List(-1, 7, Uid);
            });
        }

        [Test]
        public void List_InvalidPageIndex()
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.List(10, -1, Uid);
            });
        }

        [Test]
        public void ListByTag_TagIdOutOfRange()
        {
            var svc = CreateService();
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListByTag(-35, 996, 251);
            });
        }

        [TestCase(-996)]
        [TestCase(12306)]
        public void ListArchive_InvalidYear(int year)
        {
            var svc = CreateService();
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListArchive(year, 1);
            });
        }

        [TestCase(-35)]
        [TestCase(251)]
        public void ListArchive_InvalidMonth(int month)
        {
            var svc = CreateService();
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListArchive(996, month);
            });
        }

        [Test]
        public async Task ListArchive_OK()
        {
            var svc = CreateService();
            await svc.ListArchive(996, 9);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>(), true
            ));

            Assert.Pass();
        }

        [Test]
        public async Task ListByTag_OK()
        {
            var svc = CreateService();
            var result = await svc.ListByTag(35, 996, 251);

            _mockPostTagEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostTagSpec>(), It.IsAny<Expression<Func<PostTagEntity, PostDigest>>>(), true));
        }

        [Test]
        public async Task ListFeatured_OK()
        {
            var svc = CreateService();
            var result = await svc.ListFeatured(7, 404);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<FeaturedPostSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>(), true));
        }

        [Test]
        public async Task GetArchiveAsync_NoPosts()
        {
            _mockPostEntityRepo.Setup(p => p.Any(p => p.IsPublished && !p.IsDeleted)).Returns(false);
            var service = CreateService();

            var result = await service.GetArchiveAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetArchiveAsync_HasPosts()
        {
            _mockPostEntityRepo.Setup(p => p.Any(p => p.IsPublished && !p.IsDeleted)).Returns(true);
            var service = CreateService();

            await service.GetArchiveAsync();

            _mockPostEntityRepo.Verify(p => p.SelectAsync(
                It.IsAny<PostSpec>(),
                It.IsAny<Expression<Func<PostEntity, (int Year, int Month)>>>(),
                It.IsAny<Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>>>(),
                true));

            Assert.Pass();
        }
    }
}
