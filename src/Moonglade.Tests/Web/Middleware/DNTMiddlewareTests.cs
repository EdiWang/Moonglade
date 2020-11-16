using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moonglade.Web.Middleware;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class DNTMiddlewareTests
    {
        [Test]
        public async Task DNTHeader()
        {
            var headersArray = new Dictionary<string, StringValues> { { "DNT", "1" } };

            var reqMock = new Mock<HttpRequest>();
            reqMock.SetupGet(r => r.Headers).Returns(new HeaderDictionary(headersArray));

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(c => c.Request).Returns(reqMock.Object);
            httpContextMock.Setup(c => c.Items).Returns(new Dictionary<object, object>());

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new DNTMiddleware(RequestDelegate);

            await middleware.Invoke(httpContextMock.Object);

            Assert.AreEqual(true, httpContextMock.Object.Items["DNT"]);
        }
    }
}
