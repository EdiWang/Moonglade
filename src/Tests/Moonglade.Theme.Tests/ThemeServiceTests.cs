using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;

namespace Moonglade.Theme.Tests
{
    [TestFixture]
    public class ThemeServiceTests
    {
        private MockRepository _mockRepository;
        private Mock<IRepository<BlogThemeEntity>> _mockThemeRepository;

        [SetUp]
        public void SetUp()
        {
            _mockRepository = new(MockBehavior.Default);

            _mockThemeRepository = _mockRepository.Create<IRepository<BlogThemeEntity>>();
        }

        private ThemeService CreateService()
        {
            return new(_mockThemeRepository.Object);
        }

        [Test]
        public async Task GetAllSegment_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.GetAllSegment();

            // Assert
            _mockThemeRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<BlogThemeEntity, ThemeSegment>>>()));
            Assert.Pass();
        }

        //[Test]
        //public async Task Create_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var service = this.CreateService();
        //    string name = null;
        //    IDictionary cssRules = null;

        //    // Act
        //    var result = await service.Create(
        //        name,
        //        cssRules);

        //    // Assert
        //    Assert.Fail();
        //    this.mockRepository.VerifyAll();
        //}

        //[Test]
        //public async Task GetStyleSheet_StateUnderTest_ExpectedBehavior()
        //{
        //    // Arrange
        //    var service = this.CreateService();
        //    int id = 0;

        //    // Act
        //    var result = await service.GetStyleSheet(
        //        id);

        //    // Assert
        //    Assert.Fail();
        //    this.mockRepository.VerifyAll();
        //}

        [Test]
        public async Task Delete_Done()
        {
            // Arrange
            _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
                .Returns(ValueTask.FromResult(new BlogThemeEntity()
                {
                    Id = 996,
                    ThemeName = "996",
                    CssRules = string.Empty,
                    ThemeType = ThemeType.User
                }));
            var service = CreateService();
            int id = 996;

            // Act
            var result = await service.Delete(id);

            // Assert
            _mockThemeRepository.Verify(p => p.DeleteAsync(It.IsAny<int>()));
            Assert.AreEqual(OperationCode.Done, result);
        }

        [Test]
        public async Task Delete_SystemTheme()
        {
            // Arrange
            _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
                .Returns(ValueTask.FromResult(new BlogThemeEntity()
                {
                    Id = 996,
                    ThemeName = "996",
                    CssRules = string.Empty,
                    ThemeType = ThemeType.System
                }));
            var service = CreateService();
            int id = 996;

            // Act
            var result = await service.Delete(id);

            // Assert
            _mockThemeRepository.Verify(p => p.DeleteAsync(It.IsAny<int>()), Times.Never);
            Assert.AreEqual(OperationCode.Canceled, result);
        }

        [Test]
        public async Task Delete_NotFound()
        {
            // Arrange
            _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
                .Returns(ValueTask.FromResult((BlogThemeEntity)null));
            var service = CreateService();
            int id = 996;

            // Act
            var result = await service.Delete(id);

            // Assert
            _mockThemeRepository.Verify(p => p.DeleteAsync(It.IsAny<int>()), Times.Never);
            Assert.AreEqual(OperationCode.ObjectNotFound, result);
        }
    }
}
