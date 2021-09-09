using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.PostFeature;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]

    public class FeaturedModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockMediator = _mockRepository.Create<IMediator>();

            _mockBlogConfig.Setup(p => p.ContentSettings).Returns(new ContentSettings
            {
                PostListPageSize = 10
            });
        }

        private FeaturedModel CreateFeaturedModel()
        {
            return new(
                _mockBlogConfig.Object,
                _mockBlogCache.Object,
                _mockMediator.Object);
        }

        [Test]
        public async Task OnGet_StateUnderTest_ExpectedBehavior()
        {
            _mockMediator.Setup(p => p.Send(It.IsAny<ListFeaturedQuery>(), default))
                .Returns(Task.FromResult(FakeData.FakePosts));

            _mockBlogCache.Setup(p =>
                    p.GetOrCreateAsync(CacheDivision.PostCountFeatured, It.IsAny<string>(), It.IsAny<Func<ICacheEntry, Task<int>>>()))
                .Returns(Task.FromResult(FakeData.Int1));


            // Arrange
            var featuredModel = CreateFeaturedModel();
            int p = 1;

            // Act
            await featuredModel.OnGet(p);

            // Assert
            Assert.IsNotNull(featuredModel.Posts);
            Assert.AreEqual(FakeData.Int1, featuredModel.Posts.TotalItemCount);
        }
    }
}
