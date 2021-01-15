using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Tests.Controllers
{
    [TestFixture]
    public class HomeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IPostService> _mockPostService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ILogger<HomeController>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockPostService = _mockRepository.Create<IPostService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockLogger = _mockRepository.Create<ILogger<HomeController>>();
        }

        private HomeController CreateHomeController()
        {
            return new(
                _mockPostService.Object,
                _mockBlogCache.Object,
                _mockBlogConfig.Object,
                _mockLogger.Object);
        }
    }
}
