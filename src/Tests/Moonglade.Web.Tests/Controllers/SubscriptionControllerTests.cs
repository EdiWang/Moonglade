using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core;
using Moonglade.Syndication;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
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
            _mockRepository = new(MockBehavior.Default);

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

        [Test]
        public async Task Opml_Content()
        {
            IReadOnlyList<Category> cats = new List<Category>
            {
                new()
                {
                    Id = Guid.Empty, DisplayName = FakeData.Title3, Note = "This is fubao", RouteName = FakeData.Slug2
                }
            };

            _mockCategoryService.Setup(p => p.GetAllAsync()).Returns(Task.FromResult(cats));
            _mockBlogConfig.Setup(p => p.GeneralSettings).Returns(new GeneralSettings
            {
                CanonicalPrefix = FakeData.Url1,
                SiteTitle = "996 ICU"
            });

            var ctl = CreateSubscriptionController();
            ctl.ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await ctl.Opml();
            Assert.IsInstanceOf<ContentResult>(result);
            Assert.AreEqual("text/xml", ((ContentResult)result).ContentType);
        }
    }
}
