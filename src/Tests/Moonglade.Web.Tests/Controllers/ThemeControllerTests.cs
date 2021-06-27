using Moonglade.Caching;
using Moonglade.Configuration;
using Moonglade.Theme;
using Moonglade.Web.Controllers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moonglade.Data;
using Moonglade.Web.Models.Settings;

namespace Moonglade.Web.Tests.Controllers
{
    [TestFixture]
    public class ThemeControllerTests
    {
        private MockRepository _mockRepository;

        private Mock<IThemeService> _mockThemeService;
        private Mock<IBlogCache> _mockBlogCache;
        private Mock<IBlogConfig> _mockBlogConfig;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockThemeService = _mockRepository.Create<IThemeService>();
            _mockBlogCache = _mockRepository.Create<IBlogCache>();
            _mockBlogConfig = _mockRepository.Create<IBlogConfig>();
        }

        private ThemeController CreateThemeController()
        {
            return new(
                _mockThemeService.Object,
                _mockBlogCache.Object,
                _mockBlogConfig.Object);
        }

        //[Test]
        //public async Task Css_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var themeController = CreateThemeController();

        //    // Act
        //    var result = await themeController.Css();

        //    // Assert
        //    Assert.Fail();
        //    _mockRepository.VerifyAll();
        //}

        [Test]
        public async Task CreateTheme_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            _mockThemeService.Setup(p => p.Create(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>()))
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
            _mockThemeService.Verify(p => p.Create("996", It.IsAny<IDictionary<string, string>>()));
        }

        [Test]
        public async Task DeleteTheme_OK()
        {
            // Arrange
            _mockThemeService.Setup(p => p.Delete(It.IsAny<int>())).Returns(Task.FromResult(OperationCode.Done));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<NoContentResult>(result);
        }

        [Test]
        public async Task DeleteTheme_NotFound()
        {
            // Arrange
            _mockThemeService.Setup(p => p.Delete(It.IsAny<int>())).Returns(Task.FromResult(OperationCode.ObjectNotFound));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<NotFoundResult>(result);
        }

        [Test]
        public async Task DeleteTheme_BadRequest()
        {
            // Arrange
            _mockThemeService.Setup(p => p.Delete(It.IsAny<int>())).Returns(Task.FromResult(OperationCode.Canceled));
            var themeController = CreateThemeController();
            int id = 996;

            // Act
            var result = await themeController.Delete(id);

            // Assert
            Assert.IsInstanceOf<BadRequestObjectResult>(result);
        }
    }
}
