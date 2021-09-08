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
    public class PostFeatureTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<PostEntity>> _mockPostEntityRepo;
        private Mock<IRepository<PostTagEntity>> _mockPostTagEntityRepo;
        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
        private Mock<IBlogCache> _mockBlogCache;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockPostEntityRepo = _mockRepository.Create<IRepository<PostEntity>>();
            _mockPostTagEntityRepo = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
        }

        [Test]
        public async Task GetPostByIdQueryHandler_OK()
        {
            var handler = new GetPostByIdQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(Uid), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>()));
        }

        [Test]
        public async Task GetPostBySlugQueryHandler_OK()
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
        public async Task GetDraftQueryHandler_OK()
        {
            var handler = new GetDraftQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(Uid), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>()));
        }

        [Test]
        public async Task ListPostSegmentByStatusQueryHandler_OK()
        {
            var handler = new ListPostSegmentByStatusQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(PostStatus.Published), default);

            _mockPostEntityRepo.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>()));
        }

        [TestCase(-1, 0)]
        [TestCase(-1, 1)]
        public void ListPostSegmentQueryHandler_InvalidParameter(int offset, int pageSize)
        {
            var handler = new ListPostSegmentQueryHandler(_mockPostEntityRepo.Object);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(PostStatus.Published, offset, pageSize), default);
            });
        }

        [TestCase(PostStatus.Published)]
        [TestCase(PostStatus.Deleted)]
        [TestCase(PostStatus.Draft)]
        [TestCase(PostStatus.Default)]
        public async Task ListPostSegmentQueryHandler_WithPaging(PostStatus postStatus)
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

            var handler = new ListPostSegmentQueryHandler(_mockPostEntityRepo.Object);
            var result = await handler.Handle(new(postStatus, 0, 35), default);

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
            var handler = new ListPostsQueryHandler(_mockPostEntityRepo.Object);
            await handler.Handle(new(35, 7, Uid), default);

            _mockPostEntityRepo.Verify(p => p.SelectAsync(It.IsAny<PostPagingSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>()));
        }

        [Test]
        public void List_InvalidPageSize()
        {
            var handler = new ListPostsQueryHandler(_mockPostEntityRepo.Object);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(-1, 7, Uid), default);
            });
        }

        [Test]
        public void List_InvalidPageIndex()
        {
            var handler = new ListPostsQueryHandler(_mockPostEntityRepo.Object);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await handler.Handle(new(10, -1, Uid), default);
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
