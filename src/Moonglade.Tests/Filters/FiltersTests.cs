using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MemoryCache.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using NUnit.Framework;

namespace Moonglade.Tests.Filters
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FiltersTests
    {
        [Test]
        public void AppendMoongladeVersionAttribute_OK()
        {
            var ctx = CreateResultExecutingContext(null);

            var att = new AppendAppVersion();
            att.OnResultExecuting(ctx);

            var header = ctx.HttpContext.Response.Headers["X-Moonglade-Version"];
            Assert.IsNotNull(header);
            Assert.AreEqual(header, Helper.AppVersion);
        }

        [Test]
        public void ClearPagingCountCache_Success()
        {
            var ctx = CreateActionExecutingContext(null);
            var ctx2 = CreateActionExecutedContext(ctx);

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);
            blogCache.GetOrCreate(CacheDivision.General, "postcount", _ => 996);
            blogCache.GetOrCreate(CacheDivision.General, "ali", _ => "fubao");
            blogCache.GetOrCreate(CacheDivision.PostCountCategory, "pdd", _ => 007);
            blogCache.GetOrCreate(CacheDivision.PostCountTag, "hw", _ => 251);

            var att = new ClearPagingCountCache(blogCache);
            att.OnActionExecuted(ctx2);

            var postcount = mockedCache.Get<int>("General-postcount");
            var ali = mockedCache.Get<string>("General-ali");
            var pdd = mockedCache.Get<int>("PostCountCategory-pdd");
            var hw = mockedCache.Get<int>("PostCountTag-hw");

            Assert.AreEqual(0, postcount);
            Assert.AreEqual("fubao", ali);
            Assert.AreEqual(0, pdd);
            Assert.AreEqual(0, hw);
        }

        #region Helper Methods

        // https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/shared/Mvc.Core.TestCommon/CommonFilterTest.cs

        private static ResultExecutingContext CreateResultExecutingContext(IFilterMetadata filter)
        {
            return new ResultExecutingContext(
                CreateActionContext(),
                new IFilterMetadata[] { filter, },
                new NoOpResult(),
                controller: new object());
        }

        private static ActionExecutingContext CreateActionExecutingContext(IFilterMetadata filter)
        {
            return new ActionExecutingContext(
                CreateActionContext(),
                new IFilterMetadata[] { filter, },
                new Dictionary<string, object>(),
                controller: new object());
        }

        private static ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext context)
        {
            return new ActionExecutedContext(context, context.Filters, context.Controller)
            {
                Result = context.Result,
            };
        }

        private static ActionContext CreateActionContext()
        {
            return new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        }

        private class NoOpResult : IActionResult
        {
            public Task ExecuteResultAsync(ActionContext context)
            {
                return Task.FromResult(true);
            }
        }

        #endregion
    }
}
