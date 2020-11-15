using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moonglade.Core;
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

            var att = new AppendMoongladeVersion();
            att.OnResultExecuting(ctx);

            var header = ctx.HttpContext.Response.Headers["X-Moonglade-Version"];
            Assert.IsNotNull(header);
            Assert.AreEqual(header, Utils.AppVersion);
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
