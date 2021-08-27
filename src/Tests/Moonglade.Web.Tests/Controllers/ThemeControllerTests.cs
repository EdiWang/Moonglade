using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Data;
using Moonglade.Theme;
using Moonglade.Web.Controllers;
using Moonglade.Web.Models.Settings;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class ThemeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IMediator> _mockMediator;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);
            _mockMediator = _mockRepository.Create<IMediator>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private ThemeController CreateThemeController()
        {
            return new(
                _mockMediator.Object,
                _mockBlogCache.Object,
                _mockBlogConfig.Object);
        }

        [Test]
        public async Task Css_NotFound()
        {
            // Arrange
            _mockBlogCache.Setup(p =>
                p.GetOrCreateAsync<string>(CacheDivision.General, "theme", It.IsAny<Func<ICacheEntry, Task<string>>>())).Returns(Task.FromResult((string)null));

            var themeController = CreateThemeController();

            // Act
            var result = await themeController.Css();

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Css_Conflict()
        {
            // Arrange
            _mockBlogCache.Setup(p =>
                p.GetOrCreateAsync<string>(CacheDivision.General, "theme", It.IsAny<Func<ICacheEntry, Task<string>>>())).Returns(Task.FromResult("hahahahaha"));

            var themeController = CreateThemeController();

            // Act
            var result = await themeController.Css();

            // Assert
            Assert.IsInstanceOf<ConflictObjectResult>(result);
        }

        [Test]
        public async Task Css_Content()
        {
            // Arrange
            _mockBlogCache.Setup(p =>
                p.GetOrCreateAsync<string>(CacheDivision.General, "theme", It.IsAny<Func<ICacheEntry, Task<string>>>())).Returns(Task.FromResult(":root{--accent-color1:#2a579a;--accent-color2:#1a365f;--accent-color3:#3e6db5}"));

            var themeController = CreateThemeController();

            // Act
            var result = await themeController.Css();

            // Assert
            Assert.IsInstanceOf<ContentResult>(result);
        }

        [Test]
        public async Task Create_OK()
        {
            // Arrange
            _mockMediator.Setup(p => p.Send(It.IsAny<CreateThemeCommand>(), default))
                .Returns(Task.FromResult(1));
            var themeController = CreateThemeController();
            CreateThemeRequest request = new()
            {
                Name = "996",
                AccentColor1 = "#996",
                AccentColor2 = "#007",
                AccentColor3 = "#404",
            };

            // Act
            var result = await themeController.Create(request);

            // Assert
            Assert.IsInstanceOf<OkObjectResult>(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<CreateThemeCommand>(), default));
        }

        [Test]
        public async Task Create_Conflict()
        {
            // Arrange
            _mockMediator.Setup(p => p.Send(It.IsAny<CreateThemeCommand>(), default))
                 .Returns(Task.FromResult(0));
            var themeController = CreateThemeController();
            CreateThemeRequest request = new()
            {
                Name = "996",
                AccentColor1 = "#996",
                AccentColor2 = "#007",
                AccentColor3 = "#404",
            };

            // Act
            var result = await themeController.Create(request);

            // Assert
            Assert.IsInstanceOf<ConflictObjectResult>(result);
            _mockMediator.Verify(p => p.Send(It.IsAny<CreateThemeCommand>(), default));
        }

        [Test]
        public async Task Delete_OK()
        {
            // Arrange
            _mockMediator.Setup(p => p.Send(It.IsAny<DeleteThemeCommand>(), default)).Returns(Task.FromResult(OperationCode.Done));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task Delete_NotFound()
        {
            // Arrange
            _mockMediator.Setup(p => p.Send(It.IsAny<DeleteThemeCommand>(), default)).Returns(Task.FromResult(OperationCode.ObjectNotFound));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task Delete_BadRequest()
        {
            // Arrange
            _mockMediator.Setup(p => p.Send(It.IsAny<DeleteThemeCommand>(), default)).Returns(Task.FromResult(OperationCode.Canceled));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
    }
}
