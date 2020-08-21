using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web
{
    [TestFixture]
    public class RobotsTxtMiddlewareTests
    {
        [Test]
        public async Task TestNonRobotsTxtRequestPath()
        {
            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Path).Returns("/996");

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new RobotsTxtMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object, null);

            Assert.Pass();
        }
    }
}
