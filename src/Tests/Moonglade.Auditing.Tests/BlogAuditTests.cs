using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Configuration.Settings;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Auditing.Tests
{
    [TestFixture]
    public class BlogAuditTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<BlogAudit>> _mockLogger;
        private Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private Mock<IFeatureManager> _mockFeatureManager;
        private Mock<IRepository<AuditLogEntity>> _mockAuditLogRepo;


        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<BlogAudit>>();
            _mockHttpContextAccessor = _mockRepository.Create<IHttpContextAccessor>();
            _mockFeatureManager = _mockRepository.Create<IFeatureManager>();
            _mockAuditLogRepo = _mockRepository.Create<IRepository<AuditLogEntity>>();
        }

        private BlogAudit CreateBlogAudit()
        {
            return new BlogAudit(
                _mockLogger.Object,
                _mockHttpContextAccessor.Object,
                _mockFeatureManager.Object,
                _mockAuditLogRepo.Object);
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
