using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Moonglade.Web.Configuration;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Configuration
{
    [TestFixture]
    public class ConfigureStatusCodePagesTests
    {
        [Test]
        public async Task Handler_OK()
        {
            var ctx = new DefaultHttpContext();
            ctx.HttpContext.Response.StatusCode = 404;

            var context = new StatusCodeContext(ctx, new(),
                _ => Task.CompletedTask);

            var task = ConfigureStatusCodePages.Handler(context);
            await task;

            Assert.AreEqual("application/json; charset=utf-8", ctx.HttpContext.Response.ContentType);
        }
    }
}
