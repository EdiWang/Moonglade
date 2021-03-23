using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Auth;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.ImageStorage;
using Moonglade.Pages;
using Moonglade.Web;
using Moonglade.Web.BlogProtocols;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Configuration;

namespace Moonglade.Web.Tests.BlogProtocols
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

        private readonly string _key = "work996andgetintoicu";
        private readonly string _username = "programmer";
        private readonly string _password = "work996";

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new MockRepository(MockBehavior.Default);

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

            _mockOptions.Setup(p => p.Value).Returns(new AuthenticationSettings
            {
                MetaWeblog = new MetaWeblogCredential
                {
                    Username = _username,
                    Password = _password
                }
            });
        }

        private MetaWeblogService CreateService()
        {
            return new MetaWeblogService(
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

        [Test]
        public async Task GetUserInfoAsync_StateUnderTest_ExpectedBehavior()
        {
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                OwnerEmail = "fubao@996.icu",
                OwnerName = "996 Worker",
                CanonicalPrefix = "https://996.icu"
            });

            var service = CreateService();

            var result = await service.GetUserInfoAsync(_key, _username, _password);
            Assert.IsNotNull(result);
        }
    }
}
