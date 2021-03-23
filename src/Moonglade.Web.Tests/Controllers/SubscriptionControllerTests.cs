using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Syndication;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class SubscriptionControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<ISyndicationService> _mockSyndicationService;
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IOpmlWriter> _mockOpmlWriter;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);

            _mockSyndicationService = _mockRepository.Create<ISyndicationService>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockOpmlWriter = _mockRepository.Create<IOpmlWriter>();
        }

        private SubscriptionController CreateSubscriptionController()
        {
            return new(
                _mockSyndicationService.Object,
                _mockCategoryService.Object,
                _mockBlogConfig.Object,
                _mockBlogCache.Object,
                _mockOpmlWriter.Object);
        }

        
    }
}
