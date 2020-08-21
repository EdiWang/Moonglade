using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using NUnit.Framework;

namespace Moonglade.Tests.Web.Middleware
{
    [TestFixture]
    public class FirstRunMiddlewareTests
    {
        [Test]
        public async Task TestFirstRunWithToken()
        {
            var ctx = new DefaultHttpContext();
            AppDomain.CurrentDomain.SetData("FIRSTRUN_INIT_SUCCESS", "TRUE");

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new FirstRunMiddleware(RequestDelegate);
            await middleware.Invoke(ctx, null, null, null);

            Assert.Pass();
        }
    }
}
