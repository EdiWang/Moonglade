using Moonglade.Pingback;
using Moq;
using NUnit.Framework;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    public class PingbackResultTests
    {
        private static readonly int DefaultCharacterChunkSize = 16 * 1024;

        private MockRepository _mockRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
        }

        [Test]
        public async Task ExecuteResultAsync_Success()
        {
            // Arrange
            var pingbackResult = new PingbackResult(PingbackResponse.Success);
            var httpContext = GetHttpContext();
            ActionContext context = GetActionContext(httpContext);

            // Act
            await pingbackResult.ExecuteResultAsync(context);

            Assert.AreEqual("text/xml", context.HttpContext.Response.ContentType);
            Assert.AreEqual(201, context.HttpContext.Response.StatusCode);
        }

        [Test]
        public async Task ExecuteResultAsync_Error17SourceNotContainTargetUri()
        {
            // Arrange
            var pingbackResult = new PingbackResult(PingbackResponse.Error17SourceNotContainTargetUri);
            var httpContext = GetHttpContext();
            ActionContext context = GetActionContext(httpContext);

            // Act
            await pingbackResult.ExecuteResultAsync(context);

            Assert.AreEqual("text/xml", context.HttpContext.Response.ContentType);
            Assert.AreEqual(201, context.HttpContext.Response.StatusCode);
        }

        // https://source.dot.net/#Microsoft.AspNetCore.Mvc.Core.Test/ContentResultTest.cs
        private static ActionContext GetActionContext(HttpContext httpContext)
        {
            var routeData = new RouteData();
            routeData.Routers.Add(Mock.Of<IRouter>());

            return new ActionContext(httpContext,
                routeData,
                new ActionDescriptor());
        }

        private static IServiceCollection CreateServices()
        {
            // An array pool could return a buffer which is greater or equal to the size of the default character
            // chunk size. Since the tests here depend on a specific character buffer size to test boundary conditions,
            // make sure to only return a buffer of that size.
            var charArrayPool = new Mock<ArrayPool<char>>();
            charArrayPool
                .Setup(ap => ap.Rent(DefaultCharacterChunkSize))
                .Returns(new char[DefaultCharacterChunkSize]);

            var services = new ServiceCollection();
            services.AddSingleton<IActionResultExecutor<ContentResult>>(new ContentResultExecutor(
                new Logger<ContentResultExecutor>(NullLoggerFactory.Instance),
                new MemoryPoolHttpResponseStreamWriterFactory(ArrayPool<byte>.Shared, charArrayPool.Object)));
            return services;
        }

        private static HttpContext GetHttpContext()
        {
            var services = CreateServices();

            var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };

            return httpContext;
        }
    }

    internal class MemoryPoolHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
    {
        /// <summary>
        /// The default size of buffers <see cref="HttpResponseStreamWriter"/>s will allocate.
        /// </summary>
        /// <value>
        /// 16K causes each <see cref="HttpResponseStreamWriter"/> to allocate one 16K
        /// <see langword="char"/> array and one 32K (for UTF8) <see langword="byte"/> array.
        /// </value>
        /// <remarks>
        /// <see cref="MemoryPoolHttpResponseStreamWriterFactory"/> maintains <see cref="ArrayPool{T}"/>s
        /// for these arrays.
        /// </remarks>
        public static readonly int DefaultBufferSize = 16 * 1024;

        private readonly ArrayPool<byte> _bytePool;
        private readonly ArrayPool<char> _charPool;

        /// <summary>
        /// Creates a new <see cref="MemoryPoolHttpResponseStreamWriterFactory"/>.
        /// </summary>
        /// <param name="bytePool">
        /// The <see cref="ArrayPool{Byte}"/> for creating <see cref="byte"/> buffers.
        /// </param>
        /// <param name="charPool">
        /// The <see cref="ArrayPool{Char}"/> for creating <see cref="char"/> buffers.
        /// </param>
        public MemoryPoolHttpResponseStreamWriterFactory(
            ArrayPool<byte> bytePool,
            ArrayPool<char> charPool)
        {
            _bytePool = bytePool ?? throw new ArgumentNullException(nameof(bytePool));
            _charPool = charPool ?? throw new ArgumentNullException(nameof(charPool));
        }

        /// <inheritdoc />
        public TextWriter CreateWriter(Stream stream, Encoding encoding)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize, _bytePool, _charPool);
        }
    }
}
