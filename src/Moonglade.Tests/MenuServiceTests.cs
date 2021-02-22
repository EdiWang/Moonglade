using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Menus;
using Moq;
using NUnit.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Moonglade.Tests
{
    [TestFixture]
    [ExcludeFromCodeCoverage]
    public class MenuServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<MenuService>> _mockLogger;
        private Mock<IRepository<MenuEntity>> _mockMenuRepository;
        private Mock<IBlogAudit> _mockBlogAudit;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockLogger = _mockRepository.Create<ILogger<MenuService>>();
            _mockMenuRepository = _mockRepository.Create<IRepository<MenuEntity>>();
            _mockBlogAudit = _mockRepository.Create<IBlogAudit>();
        }

        private MenuService CreateService()
        {
            return new(
                _mockLogger.Object,
                _mockMenuRepository.Object,
                _mockBlogAudit.Object);
        }

        [Test]
        public async Task GetAsync_Null()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((MenuEntity)null));

            var ctl = CreateService();
            var result = await ctl.GetAsync(Guid.Empty);

            Assert.IsNull(result);
        }
    }
}
