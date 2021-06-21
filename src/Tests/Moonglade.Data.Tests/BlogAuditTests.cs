using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Data.Spec;
using Moq;
using NUnit.Framework;

namespace Moonglade.Data.Tests
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
            return new(
                _mockLogger.Object,
                _mockHttpContextAccessor.Object,
                _mockFeatureManager.Object,
                _mockAuditLogRepo.Object);
        }

        [Test]
        public async Task AddAuditEntry_AuditLogDisabled()
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync("EnableAudit"))
                .Returns(Task.FromResult(false));

            var blogAudit = CreateBlogAudit();
            await blogAudit.AddEntry(
                BlogEventType.General,
                BlogEventId.GeneralOperation,
                "Work 996 and get into ICU");

            _mockAuditLogRepo.Verify(p => p.AddAsync(It.IsAny<AuditLogEntity>()), Times.Never);
        }

        [Test]
        public async Task AddAuditEntry_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            _mockFeatureManager.Setup(p => p.IsEnabledAsync("EnableAudit"))
                .Returns(Task.FromResult(true));
            var blogAudit = CreateBlogAudit();
            BlogEventType blogEventType = BlogEventType.General;
            BlogEventId auditEventId = BlogEventId.SettingsSavedGeneral;
            string message = "Work 996 sick ICU";

            // Act
            await blogAudit.AddEntry(
                blogEventType,
                auditEventId,
                message);

            // Assert
            _mockAuditLogRepo.Verify(p => p.AddAsync(It.IsAny<AuditLogEntity>()));
        }

        [Test]
        public void AddAuditEntry_StateUnderTest_CoverException()
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync("EnableAudit"))
                .Returns(Task.FromResult(true));
            _mockAuditLogRepo.Setup(p => p.AddAsync(It.IsAny<AuditLogEntity>())).Throws<InvalidOperationException>();
            var blogAudit = CreateBlogAudit();
            BlogEventType blogEventType = BlogEventType.General;
            BlogEventId auditEventId = BlogEventId.SettingsSavedGeneral;
            string message = "Work 996 sick ICU";

            Assert.DoesNotThrowAsync(async () =>
            {
                await blogAudit.AddEntry(
                    blogEventType,
                    auditEventId,
                    message);
            });
        }

        [Test]
        public async Task GetAuditEntries_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            IReadOnlyList<AuditLogEntity> list = new List<AuditLogEntity>();
            _mockAuditLogRepo.Setup(p => p.Count((ISpecification<AuditLogEntity>)null)).Returns(251);
            _mockAuditLogRepo.Setup(p => p.GetAsync(It.IsAny<AuditPagingSpec>())).Returns(Task.FromResult(list));
            var blogAudit = CreateBlogAudit();
            int skip = 35;
            int take = 996;

            // Act
            var result = await blogAudit.GetAuditEntries(
                skip,
                take);

            // Assert
            Assert.AreEqual(251, result.Count);
            Assert.IsNotNull(result.Entries);
        }

        [Test]
        public async Task ClearAuditLog_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            _mockFeatureManager.Setup(p => p.IsEnabledAsync("EnableAudit"))
                .Returns(Task.FromResult(true));
            var blogAudit = CreateBlogAudit();

            // Act
            await blogAudit.ClearAuditLog();

            // Assert
            _mockAuditLogRepo.Verify(p => p.ExecuteSqlRawAsync(It.IsAny<string>()));
            _mockAuditLogRepo.Verify(p => p.AddAsync(It.IsAny<AuditLogEntity>()));
        }
    }
}
