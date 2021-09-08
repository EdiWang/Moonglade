using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Core.PostFeature;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    public class PostQueryServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<IRepository<PostTagEntity>> _mockPostTagEntityRepo;
        private Mock<IRepository<PostCategoryEntity>> _mockPostCategoryRepo;
        private Mock<IBlogCache> _mockBlogCache;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockPostTagEntityRepo = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockPostCategoryRepo = _mockRepository.Create<IRepository<PostCategoryEntity>>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        private PostQueryService CreateService()
        {
            return new(
                _mockPostEntityRepo.Object,
                _mockPostTagEntityRepo.Object,
                _mockPostCategoryRepo.Object);
        }

        [Test]
        public void CountPublic_OK()
        {
            var svc = CreateService();
            svc.CountPublic();

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }

        [Test]
        public void CountByCategory_OK()
        {
            var svc = CreateService();
            svc.CountByCategory(Uid);

            _mockPostCategoryRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostCategoryEntity, bool>>>()));
        }

        [Test]
        public void CountByTag_OK()
        {
            var svc = CreateService();
            svc.CountByTag(996);

            _mockPostTagEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostTagEntity, bool>>>()));
        }

        [Test]
        public void CountByFeatured_OK()
        {
            var svc = CreateService();
            svc.CountByFeatured();

            _mockPostEntityRepo.Verify(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>()));
        }

        [Test]
        public async Task GetAsync_OK()
        {
            var handler = new GetPostByIdQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(Uid), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>()));
        }

        [Test]
        public async Task GetAsync_Slug_OK()
        {
            _mockPostEntityRepo
                .Setup(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(),
                    It.IsAny<Expression<Func<PostEntity, Guid>>>())).Returns(Task.FromResult(Uid));

            var handler = new GetPostBySlugQueryHandler(_mockPostEntityRepo.Object, _mockBlogCache.Object,
                _mockOptionsAppSettings.Object);
            var result = await handler.Handle(new(new(996, 3, 5, "work-996-junk-35")), default);

            _mockBlogCache.Verify(p => p.GetOrCreateAsync(CacheDivision.Post, Uid.ToString(), It.IsAny<Func<ICacheEntry, Task<Post>>>()));
        }

        [Test]
        public async Task GetDraft_OK()
        {
            var handler = new GetDraftQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(Uid), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>()));
        }

        [Test]
        public async Task ListSegment_OK()
        {
            var handler = new ListPostSegmentByStatusQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(PostStatus.Published), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>()));
        }

        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        public void ListSegment_InvalidParameter(int offset, int pageSize)
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListSegmentAsync(PostStatus.Published, offset, pageSize);
            });
        }

        [TestCase(PostStatus.Published)]
        [TestCase(PostStatus.Deleted)]
        [TestCase(PostStatus.Draft)]
        [TestCase(PostStatus.Default)]
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
                    It.IsAny<Expression<Func<PostEntity, PostSegment>>>())).Returns(Task.FromResult(segments));

            _mockPostEntityRepo.Setup(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>())).Returns(996);

            var svc = CreateService();
            var result = await svc.ListSegmentAsync(postStatus, 0, 35);

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task ListInsights_OK()
        {
            var handler = new ListInsightsQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(PostInsightsType.TopRead), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostInsightsSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>()));
        }

        [Test]
        public async Task List_OK()
        {
            var svc = CreateService();
            await svc.ListAsync(35, 7, Uid);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostPagingSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>()));
        }

        [Test]
        public void List_InvalidPageSize()
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListAsync(-1, 7, Uid);
            });
        }

        [Test]
        public void List_InvalidPageIndex()
        {
            var svc = CreateService();

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await svc.ListAsync(10, -1, Uid);
            });
        }

        [Test]
        public void ListByTag_TagIdOutOfRange()
        {
            var handler = new ListByTagQueryHandler(_mockPostTagEntityRepo.Object);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(-35, 996, 251), default);
            });
        }

        [TestCase(-996)]
        [TestCase(12306)]
        public void ListArchive_InvalidYear(int year)
        {
            var handler = new ListArchiveQueryHandler(_mockPostEntityRepo.Object);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(year, 1), default);
            });
        }

        [TestCase(-35)]
        [TestCase(251)]
        public void ListArchive_InvalidMonth(int month)
        {
            var handler = new ListArchiveQueryHandler(_mockPostEntityRepo.Object);
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(996, month), default);
            });
        }

        [Test]
        public async Task ListArchive_OK()
        {
            var handler = new ListArchiveQueryHandler(_mockPostEntityRepo.Object);
            await handler.Handle(new(996, 9), default);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>()
            ));

            Assert.Pass();
        }

        [Test]
        public async Task ListByTag_OK()
        {
            var handler = new ListByTagQueryHandler(_mockPostTagEntityRepo.Object);
            var result = await handler.Handle(new(35, 996, 251), default);

            _mockPostTagEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostTagSpec>(), It.IsAny<Expression<Func<PostTagEntity, PostDigest>>>()));
        }

        [Test]
        public async Task ListFeatured_OK()
        {
            var handler = new ListFeaturedQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(7, 404), default);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<FeaturedPostSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>()));
        }

        [Test]
        public async Task GetArchiveAsync_NoPosts()
        {
            _mockPostEntityRepo.Setup(p => p.Any(p => p.IsPublished && !p.IsDeleted)).Returns(false);

            var handler = new GetArchiveQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(), default);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public async Task GetArchiveAsync_HasPosts()
        {
            _mockPostEntityRepo.Setup(p => p.Any(p => p.IsPublished && !p.IsDeleted)).Returns(true);
            var service = CreateService();

            var handler = new GetArchiveQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(), default);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(
                It.IsAny<Expression<Func<PostEntity, (int Year, int Month)>>>(),
                It.IsAny<Expression<Func<IGrouping<(int Year, int Month), PostEntity>, Archive>>>(),
                It.IsAny<PostSpec>()));

            Assert.Pass();
        }
    }
}
