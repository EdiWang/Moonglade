using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Core.CategoryFeature;
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

        private Mock<ISyndicationDataSource> _mockSyndicationService;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IOpmlWriter> _mockOpmlWriter;
        private Mock<IMediator> _mockMediator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockSyndicationService = _mockRepository.Create<ISyndicationDataSource>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockOpmlWriter = _mockRepository.Create<IOpmlWriter>();
            _mockMediator = _mockRepository.Create<IMediator>();
        }

        private SubscriptionController CreateSubscriptionController()
        {
            return new(
                _mockSyndicationService.Object,
                _mockBlogConfig.Object,
                _mockBlogCache.Object,
                _mockOpmlWriter.Object, _mockMediator.Object);
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

            _mockMediator.Setup(p => p.Send(It.IsAny<GetCategoriesQuery>(), default)).Returns(Task.FromResult(cats));
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
