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
using Microsoft.Extensions.Logging;
using Moonglade.Caching;
using Moonglade.Pingback.Mvc;
using Moonglade.Utils;
using Moonglade.Web.Filters;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Filters
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FiltersTests
    {
        [Test]
        public void AppendMoongladeVersionAttribute_OnResultExecuting()
        {
            var ctx = CreateResultExecutingContext(null);

            var att = new AppendAppVersion();
            att.OnResultExecuting(ctx);

            var header = ctx.HttpContext.Response.Headers["X-Moonglade-Version"];
            Assert.IsNotNull(header);
            Assert.AreEqual(header, Helper.AppVersion);
        }

        [Test]
        public void ClearPagingCountCache_OnActionExecuted()
        {
            var ctx = MakeActionExecutedContext();

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);
            blogCache.GetOrCreate(CacheDivision.General, "postcount", _ => 996);
            blogCache.GetOrCreate(CacheDivision.General, "ali", _ => "fubao");
            blogCache.GetOrCreate(CacheDivision.PostCountCategory, "pdd", _ => 007);
            blogCache.GetOrCreate(CacheDivision.PostCountTag, "hw", _ => 251);

            var att = new ClearPagingCountCache(blogCache);
            att.OnActionExecuted(ctx);

            var postcount = mockedCache.Get<int>("General-postcount");
            var ali = mockedCache.Get<string>("General-ali");
            var pdd = mockedCache.Get<int>("PostCountCategory-pdd");
            var hw = mockedCache.Get<int>("PostCountTag-hw");

            Assert.AreEqual(0, postcount);
            Assert.AreEqual("fubao", ali);
            Assert.AreEqual(0, pdd);
            Assert.AreEqual(0, hw);
        }

        [Test]
        public void ClearBlogCache_OnActionExecuted()
        {
            var ctx = MakeActionExecutedContext();

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);
            blogCache.GetOrCreate(CacheDivision.General, "pdd-overwork-death",
                _ => "你们看看底层的人民，哪一个不是用命换钱，" +
                     "我一直不以为是资本的问题，而是这个社会的问题，" +
                     "这是一个用命拼的时代，你可以选择安逸的日子，" +
                     "但你就要选择安逸带来的后果，" +
                     "人是可以控制自己的努力的，我们都可以");

            var att = new ClearBlogCache(CacheDivision.General, "pdd-overwork-death", blogCache);
            att.OnActionExecuted(ctx);

            var pddReply = mockedCache.Get<string>("General-pdd-overwork-death");
            Assert.AreEqual(null, pddReply);
        }

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

        [Test]
        public void ClearSiteMapCache_OnActionExecuted()
        {
            var ctx = MakeActionExecutedContext();

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);

            blogCache.GetOrCreate(CacheDivision.General, "sitemap",
                _ => "The 996 working hour system (Chinese: 996工作制) is a work schedule commonly practiced by some companies in the People's Republic of China. It derives its name from its requirement that employees work from 9:00 am to 9:00 pm, 6 days per week; i.e. 72 hours per week. A number of Chinese internet companies have adopted this system as their official work schedule. Critics argue that the 996 working hour system is a flagrant violation of Chinese law.");

            var att = new ClearSiteMapCache(blogCache);
            att.OnActionExecuted(ctx);

            var work996 = mockedCache.Get<string>("General-sitemap");
            Assert.AreEqual(null, work996);
        }

        private static ActionExecutedContext MakeActionExecutedContext()
        {
            var ctx = CreateActionExecutingContext(null);
            return CreateActionExecutedContext(ctx);
        }

        #region Helper Methods

        // https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/shared/Mvc.Core.TestCommon/CommonFilterTest.cs

        private static ResultExecutingContext CreateResultExecutingContext(IFilterMetadata filter)
        {
            return new(
                CreateActionContext(),
                new[] { filter },
                new NoOpResult(),
                controller: new());
        }

        private static ActionExecutingContext CreateActionExecutingContext(IFilterMetadata filter)
        {
            return new(
                CreateActionContext(),
                new[] { filter },
                new Dictionary<string, object>(),
                controller: new());
        }

        private static ActionExecutedContext CreateActionExecutedContext(ActionExecutingContext context)
        {
            return new(context, context.Filters, context.Controller)
            {
                Result = context.Result
            };
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

        #endregion
    }
}
