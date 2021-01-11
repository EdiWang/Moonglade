using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Moonglade.Model.Settings;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class BlogAuditTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<BlogAudit>> _mockLogger;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IFeatureManager> _mockFeatureManager;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Strict);

            _mockLogger = _mockRepository.Create<ILogger<BlogAudit>>();
            _mockConfiguration = _mockRepository.Create<IConfiguration>();
            _mockHttpContextAccessor = _mockRepository.Create<IHttpContextAccessor>();
            _mockFeatureManager = _mockRepository.Create<IFeatureManager>();
        }

        private BlogAudit CreateBlogAudit()
        {
            return new BlogAudit(
                _mockLogger.Object,
                _mockConfiguration.Object,
                _mockHttpContextAccessor.Object,
                _mockFeatureManager.Object);
        }

        [Test]
        public async Task AddAuditEntry_AuditLogDisabled()
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(false));

            var blogAudit = CreateBlogAudit();
            await blogAudit.AddAuditEntry(
                EventType.General,
                AuditEventId.GeneralOperation,
                "Work 996 and get into ICU");

            Assert.Pass();
        }

        //[Test]
        //public async Task AddAuditEntry_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var blogAudit = CreateBlogAudit();
        //    EventType eventType = default(EventType);
        //    AuditEventId auditEventId = default(AuditEventId);
        //    string message = null;

        //    // Act
        //    await blogAudit.AddAuditEntry(
        //        eventType,
        //        auditEventId,
        //        message);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task GetAuditEntries_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var blogAudit = CreateBlogAudit();
        //    int skip = 0;
        //    int take = 0;
        //    EventType? eventType = null;
        //    AuditEventId? eventId = null;

        //    // Act
        //    var result = await blogAudit.GetAuditEntries(
        //        skip,
        //        take,
        //        eventType,
        //        eventId);

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task ClearAuditLog_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var blogAudit = CreateBlogAudit();

        //    // Act
        //    await blogAudit.ClearAuditLog();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}
    }
}
