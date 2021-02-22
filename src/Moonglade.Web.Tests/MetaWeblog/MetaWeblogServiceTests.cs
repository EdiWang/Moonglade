using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Pages;
using Moonglade.Web.MetaWeblog;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.MetaWeblog
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MetaWeblogServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<IOptions<AuthenticationSettings>> _mockOptions;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<ITZoneResolver> _mockTZoneResolver;
        private Mock<ILogger<MetaWeblogService>> _mockLogger;
        private Mock<ITagService> _mockTagService;
        private Mock<ICategoryService> _mockCategoryService;
        private Mock<IPostService> _mockPostService;
        private Mock<IPageService> _mockPageService;
        private Mock<IBlogImageStorage> _mockBlogImageStorage;
        private Mock<IFileNameGenerator> _mockFileNameGenerator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockOptions = _mockRepository.Create<IOptions<AuthenticationSettings>>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockTZoneResolver = _mockRepository.Create<ITZoneResolver>();
            _mockLogger = _mockRepository.Create<ILogger<MetaWeblogService>>();
            _mockTagService = _mockRepository.Create<ITagService>();
            _mockCategoryService = _mockRepository.Create<ICategoryService>();
            _mockPostService = _mockRepository.Create<IPostService>();
            _mockPageService = _mockRepository.Create<IPageService>();
            _mockBlogImageStorage = _mockRepository.Create<IBlogImageStorage>();
            _mockFileNameGenerator = _mockRepository.Create<IFileNameGenerator>();
        }

        private MetaWeblogService CreateService()
        {
            return new(
                _mockOptions.Object,
                _mockBlogConfig.Object,
                _mockTZoneResolver.Object,
                _mockLogger.Object,
                _mockTagService.Object,
                _mockCategoryService.Object,
                _mockPostService.Object,
                _mockPageService.Object,
                _mockBlogImageStorage.Object,
                _mockFileNameGenerator.Object);
        }
    }
}
