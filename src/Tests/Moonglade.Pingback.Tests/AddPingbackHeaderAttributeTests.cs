using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class AddPingbackHeaderAttributeTests
    {
        [Test]
        public void AddPingbackHeaderAttribute_OnResultExecuting()
        {
            var ctx = CreateResultExecutingContext(null);

            var att = new AddPingbackHeaderAttribute("fubao");
            att.OnResultExecuting(ctx);

            var header = ctx.HttpContext.Response.Headers["x-pingback"];
            Assert.IsNotNull(header);
            Assert.AreEqual("https://996.icu/fubao", header);
        }

        private static ResultExecutingContext CreateResultExecutingContext(IFilterMetadata filter)
        {
            return new(
                CreateActionContext(),
                new[] { filter },
                new NoOpResult(),
                controller: new());
        }

        private static ActionContext CreateActionContext()
        {
            var httpCtx = new DefaultHttpContext();
            httpCtx.Request.Scheme = "https";
            httpCtx.Request.Host = new("996.icu");

            return new(httpCtx, new(), new());
        }

        private class NoOpResult : IActionResult
        {
            public Task ExecuteResultAsync(ActionContext context)
            {
                return Task.FromResult(true);
            }
        }
    }
}
