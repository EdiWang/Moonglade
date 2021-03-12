using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Moonglade.Pingback.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class PingSourceInspectorTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<PingSourceInspector>> _mockLogger;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockLogger = _mockRepository.Create<ILogger<PingSourceInspector>>();
        }

        private PingSourceInspector CreatePingSourceInspector()
        {
            return new(
                _mockLogger.Object);
        }

        //[Test]
        //public async Task ExamineSourceAsync_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var pingSourceInspector = this.CreatePingSourceInspector();
        //    string sourceUrl = null;
        //    string targetUrl = null;
        //    int timeoutSeconds = 0;

        //    // Act
        //    var result = await pingSourceInspector.ExamineSourceAsync(
        //        sourceUrl,
        //        targetUrl,
        //        timeoutSeconds);

        //    // Assert
        //    Assert.Fail();
        //    this.mockRepository.VerifyAll();
        //}
    }
}
