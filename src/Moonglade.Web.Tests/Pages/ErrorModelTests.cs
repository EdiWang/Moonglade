using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moonglade.Web.Pages;
using Moq;
using NUnit.Framework;

namespace Moonglade.Web.Tests.Pages
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class ErrorModelTests
    {
        private MockRepository _mockRepository;
        private Mock<ILogger<ErrorModel>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLogger = _mockRepository.Create<ILogger<ErrorModel>>();
        }

        [Test]
        public void OnGet_StateUnderTest_ExpectedBehavior()
        {
            var httpContextMock = _mockRepository.Create<HttpContext>();
            var mockIFeatureCollection = _mockRepository.Create<IFeatureCollection>();
            mockIFeatureCollection.Setup(p => p.Get<IExceptionHandlerPathFeature>())
                .Returns(new ExceptionHandlerFeature
                {
                    Path = "/996/icu",
                    Error = new("Too much fubao")
                });
            httpContextMock.Setup(p => p.Features).Returns(mockIFeatureCollection.Object);
            
            var fc = new FakeConnectionInfo { RemoteIpAddress = IPAddress.Parse("251.251.251.251") };
            httpContextMock.Setup(p => p.Connection).Returns(fc);
            httpContextMock.Setup(p => p.TraceIdentifier).Returns(FakeData.ShortString2);

            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContextMock.Object, new(), new PageActionDescriptor(), modelState);
            var modelMetadataProvider = new EmptyModelMetadataProvider();
            var viewData = new ViewDataDictionary(modelMetadataProvider, modelState);
            var tempData = new TempDataDictionary(httpContextMock.Object, Mock.Of<ITempDataProvider>());
            var pageContext = new PageContext(actionContext)
            {
                ViewData = viewData
            };

            var model = new ErrorModel(_mockLogger.Object)
            {
                PageContext = pageContext,
                TempData = tempData
            };

            model.OnGet();
            Assert.IsNotNull(model.RequestId);
        }
    }

    [ExcludeFromCodeCoverage]
    internal class FakeConnectionInfo : ConnectionInfo
    {
        public override async Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override string Id { get; set; }
        public override IPAddress? RemoteIpAddress { get; set; }
        public override int RemotePort { get; set; }
        public override IPAddress? LocalIpAddress { get; set; }
        public override int LocalPort { get; set; }
        public override X509Certificate2? ClientCertificate { get; set; }
    }
}
