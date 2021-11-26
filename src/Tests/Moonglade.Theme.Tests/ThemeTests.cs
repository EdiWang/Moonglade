using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moq;
using NUnit.Framework;
using System.Linq.Expressions;

namespace Moonglade.Theme.Tests;

[TestFixture]
public class ThemeTests
{
    private MockRepository _mockRepository;
    private Mock<IRepository<BlogThemeEntity>> _mockThemeRepository;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new(MockBehavior.Default);

        _mockThemeRepository = _mockRepository.Create<IRepository<BlogThemeEntity>>();
    }

    [Test]
    public async Task GetAllSegment_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var handler = new GetAllThemeSegmentQueryHandler(_mockThemeRepository.Object);

        // Act
        var result = await handler.Handle(new(), default);

        // Assert
        _mockThemeRepository.Verify(p => p.SelectAsync(It.IsAny<Expression<Func<BlogThemeEntity, ThemeSegment>>>()));
        Assert.Pass();
    }

    [Test]
    public async Task Create_Exists()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.Any(It.IsAny<Expression<Func<BlogThemeEntity, bool>>>())).Returns(true);
        var handler = new CreateThemeCommandHandler(_mockThemeRepository.Object);
        string name = "Honest Man";
        var cssRules = new Dictionary<string, string>();

        // Act
        var result = await handler.Handle(new(name, cssRules), default);

        // Assert
        Assert.AreEqual(0, result);
    }

    [Test]
    public void GetStyleSheet_EmptyCSS()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
            .Returns(ValueTask.FromResult(new BlogThemeEntity
            {
                Id = 996,
                ThemeName = "996",
                CssRules = string.Empty,
                ThemeType = ThemeType.User
            }));
        var handler = new GetStyleSheetQueryHandler(_mockThemeRepository.Object);
        int id = 0;

        // Act
        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            var result = await handler.Handle(new(id), default);
        });
    }

    [Test]
    public void GetStyleSheet_BadCSS()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
            .Returns(ValueTask.FromResult(new BlogThemeEntity
            {
                Id = 996,
                ThemeName = "996",
                CssRules = "work 996",
                ThemeType = ThemeType.User
            }));
        var handler = new GetStyleSheetQueryHandler(_mockThemeRepository.Object);
        int id = 0;

        // Act
        Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            var result = await handler.Handle(new(id), default);
        });
    }

    [Test]
    public async Task GetStyleSheet_GoodCSS()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
            .Returns(ValueTask.FromResult(new BlogThemeEntity
            {
                Id = 996,
                ThemeName = "996",
                CssRules = "{\"--honestman-hat\": \"green !important\"}",
                ThemeType = ThemeType.User
            }));
        var handler = new GetStyleSheetQueryHandler(_mockThemeRepository.Object);
        int id = 0;

        // Act
        var result = await handler.Handle(new(id), default);

        Assert.AreEqual(":root {--honestman-hat: green !important;}", result);
    }

    [Test]
    public async Task Delete_Done()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
            .Returns(ValueTask.FromResult(new BlogThemeEntity
            {
                Id = 996,
                ThemeName = "996",
                CssRules = string.Empty,
                ThemeType = ThemeType.User
            }));
        var handler = new DeleteThemeCommandHandler(_mockThemeRepository.Object);
        int id = 996;

        // Act
        var result = await handler.Handle(new(id), default);

        // Assert
        _mockThemeRepository.Verify(p => p.DeleteAsync(It.IsAny<int>()));
        Assert.AreEqual(OperationCode.Done, result);
    }

    [Test]
    public async Task Delete_SystemTheme()
    {
        // Arrange
        _mockThemeRepository.Setup(p => p.GetAsync(It.IsAny<int>()))
            .Returns(ValueTask.FromResult(new BlogThemeEntity
            {
                Id = 996,
                ThemeName = "996",
                CssRules = string.Empty,
                ThemeType = ThemeType.System
            }));
        var handler = new DeleteThemeCommandHandler(_mockThemeRepository.Object);
        int id = 996;

        // Act
        var result = await handler.Handle(new(id), default);

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
        var handler = new DeleteThemeCommandHandler(_mockThemeRepository.Object);
        int id = 996;

        // Act
        var result = await handler.Handle(new(id), default);

        // Assert
        _mockThemeRepository.Verify(p => p.DeleteAsync(It.IsAny<int>()), Times.Never);
        Assert.AreEqual(OperationCode.ObjectNotFound, result);
    }
}