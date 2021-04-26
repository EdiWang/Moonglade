using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Edi.Captcha;
using MemoryCache.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Web.Filters;
using Moonglade.Web.Models;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Filters
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FiltersTests
    {
        [Test]
        public void ClearPagingCountCache_OnActionExecuted()
        {
            var ctx = MakeActionExecutedContext();

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);
            blogCache.GetOrCreate(CacheDivision.General, "postcount", _ => 996);
            blogCache.GetOrCreate(CacheDivision.General, "ali", _ => FakeData.ShortString1);
            blogCache.GetOrCreate(CacheDivision.PostCountCategory, "pdd", _ => 007);
            blogCache.GetOrCreate(CacheDivision.PostCountTag, "hw", _ => FakeData.Int1);

            var att = new ClearPagingCountCache(blogCache);
            att.OnActionExecuted(ctx);

            var postcount = mockedCache.Get<int>("General-postcount");
            var ali = mockedCache.Get<string>("General-ali");
            var pdd = mockedCache.Get<int>("PostCountCategory-pdd");
            var hw = mockedCache.Get<int>("PostCountTag-hw");

            Assert.AreEqual(0, postcount);
            Assert.AreEqual(FakeData.ShortString1, ali);
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

        [Test]
        public void ClearSubscriptionCache_OnActionExecuted()
        {
            var ctx = MakeActionExecutedContext();

            var mockedCache = Create.MockedMemoryCache();
            var blogCache = new BlogMemoryCache(mockedCache);

            blogCache.GetOrCreate(CacheDivision.General, "rss", _ => "The culture of overtime work has a long history in Chinese IT companies, where the focus is typically on speed and cost reduction. Companies employ a range of measures, such as reimbursing taxi fares for employees who remain working at the office late into the night, to encourage overtime work.");
            blogCache.GetOrCreate(CacheDivision.General, "atom", _ => "On 26 March 2019, the 996.ICU repository and website were created. The repository states that the name \"996.icu\" refers to how developers who work under the 996 system (9AM–9PM, 6 days per week) would risk poor health and a possible stay in an intensive care unit. The movement's slogan is \"developers' lives matter\".");

            var att = new ClearSubscriptionCache(blogCache);
            att.OnActionExecuted(ctx);

            var work996 = mockedCache.Get<string>("General-rss");
            var sickICU = mockedCache.Get<string>("General-atom");

            Assert.AreEqual(null, work996);
            Assert.AreEqual(null, sickICU);
        }

        [Test]
        public void DisallowSpiderUA_OnActionExecuting_BrowserUA()
        {
            var ctx = CreateActionExecutingContext(null);
            ctx.HttpContext.Request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.141 Safari/537.36 Edg/87.0.664.75");

            var att = new DisallowSpiderUA();
            att.OnActionExecuting(ctx);

            Assert.IsNull(ctx.Result);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void DisallowSpiderUA_OnActionExecuting_EmptyUA(string userAgent)
        {
            var ctx = CreateActionExecutingContext(null);
            ctx.HttpContext.Request.Headers.Add("User-Agent", userAgent);

            var att = new DisallowSpiderUA();
            att.OnActionExecuting(ctx);

            Assert.IsInstanceOf<BadRequestResult>(ctx.Result);
        }

        [TestCase("Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)")]
        [TestCase("Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.96 Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)")]
        [TestCase("facebookexternalhit/1.1 (+http://www.facebook.com/externalhit_uatext.php)")]
        [TestCase("Mozilla/5.0 (compatible; MegaIndex.ru/2.0; +http://megaindex.com/crawler)")]
        public void DisallowSpiderUA_OnActionExecuting_MachineUA(string userAgent)
        {
            var ctx = CreateActionExecutingContext(null);
            ctx.HttpContext.Request.Headers.Add("User-Agent", userAgent);

            var att = new DisallowSpiderUA();
            att.OnActionExecuting(ctx);

            Assert.IsInstanceOf<ForbidResult>(ctx.Result);
        }

        [Test]
        public void ValidateCaptcha_OnActionExecuting_CaptchaNotPresent()
        {
            Mock<ISessionBasedCaptcha> mockCaptcha = new();
            var ctx = CreateActionExecutingContext(null);

            var att = new ValidateCaptcha(mockCaptcha.Object);
            att.OnActionExecuting(ctx);

            Assert.IsInstanceOf<BadRequestObjectResult>(ctx.Result);
        }

        [Test]
        public void ValidateCaptcha_OnActionExecuting_InvalidCaptcha()
        {
            Mock<ICaptchable> mockCaptchable = new();
            mockCaptchable.Setup(p => p.CaptchaCode).Returns("9960");

            Mock<ISessionBasedCaptcha> mockCaptcha = new();
            mockCaptcha.Setup(p =>
                    p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(false);

            var ctx = CreateActionExecutingContext(null, new Dictionary<string, object>
            {
                {FakeData.ShortString2, mockCaptchable.Object}
            });

            var att = new ValidateCaptcha(mockCaptcha.Object);
            att.OnActionExecuting(ctx);

            Assert.IsInstanceOf<ConflictObjectResult>(ctx.Result);
        }

        [Test]
        public void ValidateCaptcha_OnActionExecuting_ValidCaptcha()
        {
            Mock<ICaptchable> mockCaptchable = new();
            mockCaptchable.Setup(p => p.CaptchaCode).Returns("9960");

            Mock<ISessionBasedCaptcha> mockCaptcha = new();
            mockCaptcha.Setup(p =>
                    p.Validate(It.IsAny<string>(), It.IsAny<ISession>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(true);

            var ctx = CreateActionExecutingContext(null, new Dictionary<string, object>
            {
                {FakeData.ShortString2, mockCaptchable.Object}
            });

            var att = new ValidateCaptcha(mockCaptcha.Object);
            att.OnActionExecuting(ctx);

            Assert.IsNull(ctx.Result);
        }

        #region Helper Methods

        private static ActionExecutedContext MakeActionExecutedContext()
        {
            var ctx = CreateActionExecutingContext(null);
            return CreateActionExecutedContext(ctx);
        }

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

        private static ActionExecutingContext CreateActionExecutingContext(IFilterMetadata filter, IDictionary<string, object> actionArguments)
        {
            return new(
                CreateActionContext(),
                new[] { filter },
                actionArguments,
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
            httpCtx.Session = new MockHttpSession();

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
