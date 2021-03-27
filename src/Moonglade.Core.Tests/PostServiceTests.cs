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

            _mockRepositoryPostEntity.Verify(p => p.SelectFirstOrDefaultAsync(It.IsAny<PostSpec>(), It.IsAny<Expression<Func<PostEntity, Post>>>(), true));
        }

        [Test]
        public void LazyLoadToImgTag_ExistLoading()
        {
            const string html = @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>";
            var result = ContentProcessor.AddLazyLoadToImgTag(html);
            Assert.IsTrue(result == @"<p>Work 996 and have some fu bao!</p><img loading=""lazy"" src=""icu.jpg"" /><video src=""java996.mp4""></video>");
        }

        [Test]
        public void LazyLoadToImgTag_Empty()
        {
            var result = ContentProcessor.AddLazyLoadToImgTag(string.Empty);
            Assert.IsTrue(result == string.Empty);
        }
    }
}
