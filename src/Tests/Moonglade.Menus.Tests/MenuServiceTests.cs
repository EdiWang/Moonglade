using Microsoft.Extensions.Logging;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Moonglade.Menus.Tests
{
    [TestFixture]
    public class MenuServiceTests
    {
        private MockRepository _mockRepository;

        private Mock<ILogger<MenuService>> _mockLogger;
        private Mock<IRepository<MenuEntity>> _mockMenuRepository;
        private Mock<IBlogAudit> _mockBlogAudit;

        private readonly MenuEntity _menu = new()
        {
            Id = Guid.Parse("478ff468-a0cc-4f05-a5d8-b1dacdc695dd"),
            DisplayOrder = 996,
            Icon = "work-996 ",
            IsOpenInNewTab = true,
            Title = "Work 996 ",
            Url = "/work/996",
            SubMenus = new List<SubMenuEntity>
            {
                new ()
                {
                    Id = Guid.Parse("23ca73fd-a2ed-4671-84bb-16826189f4fb"),
                    MenuId = Guid.Parse("478ff468-a0cc-4f05-a5d8-b1dacdc695dd"),
                    IsOpenInNewTab = true,
                    Title = "251 Today",
                    Url = "https://251.today"
                }
            }
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

            var handler = new GetMenuQueryHandler(_mockMenuRepository.Object);
            var result = await handler.Handle(new(Guid.Empty), default);

            Assert.IsNull(result);
        }

        [Test]
        public async Task GetAllAsync_OK()
        {
            var ctl = CreateService();
            await ctl.GetAllAsync();

            _mockMenuRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<MenuEntity, Menu>>>()));
        }

        [Test]
        public async Task GetAsync_EntityToMenuModel()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_menu));

            var handler = new GetMenuQueryHandler(_mockMenuRepository.Object);
            var result = await handler.Handle(new(Guid.Empty), default);

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

        [Test]
        public async Task CreateAsync_OK()
        {
            var svc = CreateService();
            var result = await svc.CreateAsync(new()
            {
                DisplayOrder = 996,
                Icon = "work-996",
                Title = "Work 996",
                IsOpenInNewTab = true,
                Url = "work/996",
                SubMenus = new[]
                {
                    new UpdateSubMenuRequest
                    {
                        IsOpenInNewTab = true,
                        Title = "251 Today",
                        Url = "https://251.today"
                    }
                }
            });

            Assert.AreNotEqual(Guid.Empty, result);
            _mockBlogAudit.Verify(p => p.AddEntry(BlogEventType.Content, BlogEventId.MenuCreated, It.IsAny<string>()));
        }

        [Test]
        public void UpdateAsync_NullMenu()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult((MenuEntity)null));

            var svc = CreateService();

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await svc.UpdateAsync(Guid.Empty, new());
            });
        }

        [Test]
        public async Task UpdateAsync_OK()
        {
            _mockMenuRepository.Setup(p => p.GetAsync(It.IsAny<Guid>()))
                .Returns(ValueTask.FromResult(_menu));

            var svc = CreateService();
            await svc.UpdateAsync(Guid.Empty, new()
            {
                DisplayOrder = 996,
                Icon = "work-996",
                Title = "Work 996",
                IsOpenInNewTab = true,
                Url = "work/996",
                SubMenus = new[]
                {
                    new UpdateSubMenuRequest
                    {
                        IsOpenInNewTab = true,
                        Title = "251 Today",
                        Url = "https://251.today"
                    }
                }
            });

            _mockBlogAudit.Verify(p => p.AddEntry(BlogEventType.Content, BlogEventId.MenuUpdated, It.IsAny<string>()));
        }
    }
}
