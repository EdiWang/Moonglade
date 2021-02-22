using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moonglade.Auditing;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Menus;
using Moq;
using NUnit.Framework;

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

        private readonly MenuEntity _menu = new()
        {
            Id = Guid.Empty,
            DisplayOrder = 996,
            Icon = "work-996 ",
            IsOpenInNewTab = true,
            Title = "Work 996 ",
            Url = "/work/996"
        };

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

        [Test]
        public async Task GetAsync_EntityToMenuModel()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_menu));

            var ctl = CreateService();
            var result = await ctl.GetAsync(Guid.Empty);

            Assert.IsNotNull(result);
            Assert.AreEqual("work-996", result.Icon);
            Assert.AreEqual("Work 996", result.Title);
        }

        [Test]
        public void DeleteAsync_Null()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((MenuEntity)null));

            var ctl = CreateService();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ctl.DeleteAsync(Guid.Empty);
            });
        }

        [Test]
        public async Task DeleteAsync_OK()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_menu));

            var ctl = CreateService();
            await ctl.DeleteAsync(Guid.Empty);

            _mockBlogAudit.Verify();
        }
    }
}
