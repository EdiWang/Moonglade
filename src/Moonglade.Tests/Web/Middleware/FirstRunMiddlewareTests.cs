using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moonglade.Web.Middleware;
using NUnit.Framework;
using Moq;
using Moq.Dapper;
using Dapper;
using System.Data;

namespace Moonglade.Tests.Web.Middleware
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class FirstRunMiddlewareTests
    {
        private MockRepository mockRepository;

        private Mock<IDbConnection> mockDbConnection;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Default);
            mockDbConnection = mockRepository.Create<IDbConnection>();
        }

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

        [Test]
        public async Task FirstRun_DbConnection_Fail_NullLogger()
        {
            var ctx = new DefaultHttpContext();
            mockDbConnection.SetupDapper(c => c.ExecuteScalar<int>(It.IsAny<string>(), null, null, null, null)).Throws(new Exception("996"));

            static Task RequestDelegate(HttpContext context) => Task.CompletedTask;
            var middleware = new FirstRunMiddleware(RequestDelegate);
            await middleware.Invoke(ctx, mockDbConnection.Object, null, null);

            Assert.AreEqual(ctx.Response.StatusCode, 500);
        }
    }
}
