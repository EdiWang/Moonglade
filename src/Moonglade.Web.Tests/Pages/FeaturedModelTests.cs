using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FeaturedModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IPostQueryService> _mockPostQueryService;
        private Mock<IBlogCache> _mockBlogCache;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockPostQueryService = _mockRepository.Create<IPostQueryService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private FeaturedModel CreateFeaturedModel()
        {
            return new(
                _mockBlogConfig.Object,
                _mockPostQueryService.Object,
                _mockBlogCache.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockPostQueryService.Setup(p => p.ListFeatured(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult(FakeData.FakePosts));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreate(CacheDivision.PostCountFeatured, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, int>>()))
                .Returns(251);


            // Arrange
            var featuredModel = CreateFeaturedModel();
            int p = 1;

            // Act
            await featuredModel.OnGet(p);

            // Assert
            Assert.IsNotNull(featuredModel.Posts);
            Assert.AreEqual(251, featuredModel.Posts.TotalItemCount);
        }
    }
}
