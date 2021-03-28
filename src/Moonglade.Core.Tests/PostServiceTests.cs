using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auditing;
using Moonglade.Caching;
using Moonglade.Configuration.Settings;
using Moonglade.Core;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Data.Spec;
using Moonglade.Utils;

namespace Moonglade.Core.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PostServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PostService>> _mockLogger;
        private Mock<IOptions<AppSettings>> _mockOptionsAppSettings;
        private Mock<IRepository<PostEntity>> _mockRepositoryPostEntity;
        private Mock<IRepository<TagEntity>> _mockRepositoryTagEntity;
        private Mock<IRepository<PostTagEntity>> _mockRepositoryPostTagEntity;
        private Mock<IRepository<PostCategoryEntity>> _mockRepositoryPostCategoryEntity;
        private Mock<IBlogAudit> _mockBlogAudit;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IOptions<List<TagNormalization>>> _mockOptionsListTagNormalization;

        private static readonly Guid Uid = Guid.Parse("76169567-6ff3-42c0-b163-a883ff2ac4fb");

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<PostService>>();
            _mockOptionsAppSettings = _mockRepository.Create<IOptions<AppSettings>>();
            _mockRepositoryPostEntity = _mockRepository.Create<IRepository<PostEntity>>();
            _mockRepositoryTagEntity = _mockRepository.Create<IRepository<TagEntity>>();
            _mockRepositoryPostTagEntity = _mockRepository.Create<IRepository<PostTagEntity>>();
            _mockRepositoryPostCategoryEntity = _mockRepository.Create<IRepository<PostCategoryEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockOptionsListTagNormalization = _mockRepository.Create<IOptions<List<TagNormalization>>>();
        }

        private PostService CreateService()
        {
            return new(
                _mockLogger.Object,
                _mockOptionsAppSettings.Object,
                _mockRepositoryPostEntity.Object,
                _mockRepositoryTagEntity.Object,
                _mockRepositoryPostTagEntity.Object,
                _mockRepositoryPostCategoryEntity.Object,
                _mockBlogAudit.Object,
                _mockBlogCache.Object,
                _mockOptionsListTagNormalization.Object);
        }

        [Test]
        public async Task GetAsync_OK()
        {
            var svc = CreateService();
            var result = await svc.GetAsync(Uid);

            _mockRepositoryPostEntity.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>(), true));
        }

        [Test]
        public async Task GetAsync_Slug_OK()
        {
            _mockRepositoryPostEntity
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

            _mockRepositoryPostEntity.Verify(
                p => p.SelectFirstOrDefaultAsync(
                    It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>(), true));
        }

        [Test]
        public async Task ListSegment_OK()
        {
            var svc = CreateService();
            var result = await svc.ListSegment(PostStatus.Published);

            _mockRepositoryPostEntity.Verify(
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

            _mockRepositoryPostEntity
                .Setup(p => p.SelectAsync(It.IsAny<PostPagingSpec>(),
                    It.IsAny<Expression<Func<PostEntity, PostSegment>>>(), true)).Returns(Task.FromResult(segments));

            _mockRepositoryPostEntity.Setup(p => p.Count(It.IsAny<Expression<Func<PostEntity, bool>>>())).Returns(996);

            var svc = CreateService();
            var result = await svc.ListSegment(postStatus, 0, 35);

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task ListInsights_OK()
        {
            var svc = CreateService();
            var result = await svc.ListInsights(PostInsightsType.TopRead);

            _mockRepositoryPostEntity.Verify(
                p => p.SelectAsync(
                    It.IsAny<PostInsightsSpec>(), It.IsAny<Expression<Func<PostEntity, PostSegment>>>(), true));
        }

        [Test]
        public async Task List_OK()
        {
            var svc = CreateService();
            await svc.List(35, 7, Uid);

            _mockRepositoryPostEntity.Verify(p => p.SelectAsync(It.IsAny<PostPagingSpec>(), It.IsAny<Expression<Func<PostEntity, PostDigest>>>(), true));
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

        [Test]
        public async Task ListByTag_OK()
        {
            var svc = CreateService();
            var result = await svc.ListByTag(35, 996, 251);
            
            _mockRepositoryPostTagEntity.Verify(p => p.SelectAsync(It.IsAny<PostTagSpec>(), It.IsAny<Expression<Func<PostTagEntity, PostDigest>>>(), true));
        }
    }
}
