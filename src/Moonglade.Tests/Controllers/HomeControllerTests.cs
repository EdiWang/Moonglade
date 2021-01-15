using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Configuration.Abstraction;
using Moonglade.Core;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Model;

namespace Moonglade.Tests.Controllers
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
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

        [Test]
        public async Task Tags_Index()
        {
            var fakeTags = new List<DegreeTag>
            {
                new() { Degree = 251, DisplayName = "Huawei", Id = 35, NormalizedName = "aiguo" },
                new() { Degree = 996, DisplayName = "Ali", Id = 35, NormalizedName = "fubao" }
            };

            var mockTagService = new Mock<ITagService>();
            mockTagService.Setup(p => p.GetTagCountListAsync())
                .Returns(Task.FromResult((IReadOnlyList<DegreeTag>)fakeTags));

            var ctl = CreateHomeController();
            var result = await ctl.Tags(mockTagService.Object);

            Assert.IsInstanceOf<ViewResult>(result);

            Assert.AreEqual(fakeTags, ((ViewResult)result).Model);
        }
    }
}
