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
    public class MoongladeServiceTests
    {
        private Mock<ILogger<MoongladeService>> _loggerMock;
        private Mock<IOptions<AppSettings>> _appSettingsMock;

        private MoongladeService _moongladeService;

        [SetUp]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<MoongladeService>>();
            _appSettingsMock = new Mock<IOptions<AppSettings>>();

            _moongladeService = new MoongladeService(_loggerMock.Object, _appSettingsMock.Object);
        }

        [Test]
        public void TestTryExecute()
        {
            var genericResponse = _moongladeService.TryExecute(() => new Response());
            Assert.IsFalse(genericResponse.IsSuccess);
            Assert.IsTrue(genericResponse.Message == string.Empty);

            var successResponse = _moongladeService.TryExecute(() => new SuccessResponse { Message = ".NET Rocks!" });
            Assert.IsTrue(successResponse.IsSuccess);
            Assert.IsTrue(successResponse.Message == ".NET Rocks!");

            var failedResponse = _moongladeService.TryExecute(() => throw new NotSupportedException("996 is not supported"));
            Assert.IsFalse(failedResponse.IsSuccess);
            Assert.AreEqual(1, failedResponse.ResponseCode);
            Assert.AreEqual("996 is not supported", failedResponse.Message);
        }

        [Test]
        public void TestTryExecuteOfType()
        {
            var genericResponse = _moongladeService.TryExecute(() => new Response<int>(996));
            Assert.IsFalse(genericResponse.IsSuccess);
            Assert.IsTrue(genericResponse.Message == string.Empty);
            Assert.AreEqual(996, genericResponse.Item);

            var successResponse = _moongladeService.TryExecute(() => new SuccessResponse<string>("Work 955") { Message = ".NET Rocks!" });
            Assert.IsTrue(successResponse.IsSuccess);
            Assert.IsTrue(successResponse.Message == ".NET Rocks!");
            Assert.AreEqual("Work 955", successResponse.Item);

            var failedResponse = _moongladeService.TryExecute<int>(() => throw new NotSupportedException("996 is not supported"));
            Assert.IsFalse(failedResponse.IsSuccess);
            Assert.AreEqual(1, failedResponse.ResponseCode);
            Assert.AreEqual(0, failedResponse.Item);
            Assert.AreEqual("996 is not supported", failedResponse.Message);
        }
    }
}
