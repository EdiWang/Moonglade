using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Moonglade.Configuration;
using Moonglade.FriendLink;
using Moonglade.Web.Middleware;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Middleware
{
    [TestFixture]
    public class FoafMiddlewareTests
    {
        private MockRepository _mockRepository;
        private Mock<IBlogConfig> _mockBlogConfig;
        private Mock<IFoafWriter> _mockFoafWriter;
        private Mock<IFriendLinkService> _mockFriendLinkService;
        private Mock<LinkGenerator> _mockLinkGenerator;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
            _mockFoafWriter = _mockRepository.Create<IFoafWriter>();
            _mockFriendLinkService = _mockRepository.Create<IFriendLinkService>();
            _mockLinkGenerator = _mockRepository.Create<LinkGenerator>();

            _mockBlogConfig.Setup(bc => bc.GeneralSettings).Returns(new GeneralSettings
            {
                SiteTitle = "Fake Title"
            });
        }

        [Test]
        public async Task Invoke_NonFoafUrl()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new FoafMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, _mockBlogConfig.Object, _mockFoafWriter.Object, _mockFriendLinkService.Object, _mockLinkGenerator.Object);

            _mockFriendLinkService.Verify(p => p.GetAllAsync(), Times.Never);
            _mockFoafWriter.Verify(p => p.GetFoafData(It.IsAny<FoafDoc>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<Link>>()), Times.Never);
            Assert.Pass();
        }
    }
}
