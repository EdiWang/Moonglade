using Microsoft.FeatureManagement;
using Moonglade.Auditing;
using Moonglade.Web.Pages.Admin;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moonglade.Configuration.Settings;

namespace Moonglade.Web.Tests.Pages.Admin
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class AuditLogsModelTests
    {
        private MockRepository _mockRepository;

        private Mock<IFeatureManager> _mockFeatureManager;
        private Mock<IBlogAudit> _mockBlogAudit;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockFeatureManager = _mockRepository.Create<IFeatureManager>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
        }

        private AuditLogsModel CreateAuditLogsModel()
        {
            return new(
                _mockFeatureManager.Object,
                _mockBlogAudit.Object);
        }

        [Test]
        public async Task OnGetAsync_FeatureDisabled()
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(false));

            var auditLogsModel = CreateAuditLogsModel();
            int pageIndex = 0;

            var result = await auditLogsModel.OnGetAsync(pageIndex);

            Assert.IsInstanceOf<ForbidResult>(result);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task OnGetAsync_FeatureEnabled_BadPageSize(int pageIndex)
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(true));

            var auditLogsModel = CreateAuditLogsModel();
            var result = await auditLogsModel.OnGetAsync(pageIndex);

            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task AuditLogs_View()
        {
            _mockFeatureManager.Setup(p => p.IsEnabledAsync(nameof(FeatureFlags.EnableAudit)))
                .Returns(Task.FromResult(true));
            (IReadOnlyList<AuditEntry> Entries, int Count) data = new(new List<AuditEntry>(), 996);

            _mockBlogAudit.Setup(p => p.GetAuditEntries(It.IsAny<int>(), It.IsAny<int>(), null, null)).Returns(Task.FromResult(data));

            var auditLogsModel = CreateAuditLogsModel();
            var result = await auditLogsModel.OnGetAsync(1);

            Assert.IsInstanceOf<PageResult>(result);
            Assert.IsNotNull(auditLogsModel.Entries);
        }
    }
}
