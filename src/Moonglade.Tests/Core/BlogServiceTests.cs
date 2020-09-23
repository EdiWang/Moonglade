using System;
using Edi.Practice.RequestResponseModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moonglade.Core;
using Moonglade.Model.Settings;
using Moq;
using NUnit.Framework;

namespace Moonglade.Tests.Core
{
    [TestFixture]
    public class BlogServiceTests
    {
        private Mock<ILogger<BlogService>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;

        private BlogService _blogService;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<BlogService>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();

            _blogService = new BlogService(_loggerMock.Object, _appSettingsMock.Object);
        }

        [Test]
        public void TestTryExecute()
        {
            var genericResponse = _blogService.TryExecute(() => new Response());
            Assert.IsFalse(genericResponse.IsSuccess);
            Assert.IsTrue(genericResponse.Message == string.Empty);

            var successResponse = _blogService.TryExecute(() => new SuccessResponse { Message = ".NET Rocks!" });
            Assert.IsTrue(successResponse.IsSuccess);
            Assert.IsTrue(successResponse.Message == ".NET Rocks!");

            var failedResponse = _blogService.TryExecute(() => throw new NotSupportedException("996 is not supported"));
            Assert.IsFalse(failedResponse.IsSuccess);
            Assert.AreEqual(1, failedResponse.ResponseCode);
            Assert.AreEqual("996 is not supported", failedResponse.Message);
        }

        [Test]
        public void TestTryExecuteOfType()
        {
            var genericResponse = _blogService.TryExecute(() => new Response<int>(996));
            Assert.IsFalse(genericResponse.IsSuccess);
            Assert.IsTrue(genericResponse.Message == string.Empty);
            Assert.AreEqual(996, genericResponse.Item);

            var successResponse = _blogService.TryExecute(() => new SuccessResponse<string>("Work 955") { Message = ".NET Rocks!" });
            Assert.IsTrue(successResponse.IsSuccess);
            Assert.IsTrue(successResponse.Message == ".NET Rocks!");
            Assert.AreEqual("Work 955", successResponse.Item);

            var failedResponse = _blogService.TryExecute<int>(() => throw new NotSupportedException("996 is not supported"));
            Assert.IsFalse(failedResponse.IsSuccess);
            Assert.AreEqual(1, failedResponse.ResponseCode);
            Assert.AreEqual(0, failedResponse.Item);
            Assert.AreEqual("996 is not supported", failedResponse.Message);
        }
    }
}
