namespace Moonglade.ImageStorage.Tests;

public class DatedGuidFileNameGeneratorTests
{
    private readonly Guid _testGuid = new("12345678-1234-1234-1234-123456789012");
    private readonly DatedGuidFileNameGenerator _generator;

    public DatedGuidFileNameGeneratorTests()
    {
        _generator = new DatedGuidFileNameGenerator(_testGuid);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidGuid_SetsUniqueIdCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var generator = new DatedGuidFileNameGenerator(guid);

        // Assert
        Assert.Equal(guid, generator.UniqueId);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_SetsUniqueIdToEmpty()
    {
        // Act
        var generator = new DatedGuidFileNameGenerator(Guid.Empty);

        // Assert
        Assert.Equal(Guid.Empty, generator.UniqueId);
    }

    #endregion

    #region Name Property Tests

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Act & Assert
        Assert.Equal(nameof(DatedGuidFileNameGenerator), _generator.Name);
    }

    #endregion

    #region GetFileName Tests - Valid Inputs

    [Theory]
    [InlineData("test.jpg")]
    [InlineData("image.png")]
    [InlineData("document.pdf")]
    [InlineData("archive.zip")]
    public void GetFileName_WithValidFileName_ReturnsFormattedFileName(string fileName)
    {
        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var extension = Path.GetExtension(fileName);
        var expected = $"{dateStr}-{shortGuid}{extension}".ToLowerInvariant();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFileName_WithAppendixName_IncludesAppendixInResult()
    {
        // Arrange
        const string fileName = "test.jpg";
        const string appendixName = "thumbnail";

        // Act
        var result = _generator.GetFileName(fileName, appendixName);

        // Assert
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}-{appendixName}.jpg";

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("IMAGE.JPG")]
    [InlineData("Document.PDF")]
    [InlineData("Archive.ZIP")]
    public void GetFileName_WithUpperCaseExtension_ReturnsLowerCaseResult(string fileName)
    {
        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        Assert.Equal(result, result.ToLowerInvariant());
        Assert.Contains(Path.GetExtension(fileName).ToLowerInvariant(), result);
    }

    [Fact]
    public void GetFileName_WithComplexFileName_HandlesCorrectly()
    {
        // Arrange
        const string fileName = "my-complex-file-name.with.dots.jpg";

        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}.jpg";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFileName_UsesFirst8CharactersOfGuid()
    {
        // Arrange
        const string fileName = "test.jpg";

        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        var expectedGuidPart = _testGuid.ToString("N")[..8]; // "12345678"
        Assert.Contains(expectedGuidPart, result);
    }

    [Fact]
    public void GetFileName_IncludesCurrentDateInYyyyMmDdFormat()
    {
        // Arrange
        const string fileName = "test.jpg";

        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        var expectedDatePart = DateTime.UtcNow.ToString("yyyyMMdd");
        Assert.StartsWith(expectedDatePart, result);
    }

    #endregion

    #region GetFileName Tests - Invalid Inputs

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GetFileName_WithNullOrWhiteSpaceFileName_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _generator.GetFileName(fileName!));
        Assert.Equal("fileName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    public void GetFileName_WithNullOrWhiteSpaceFileName_ThrowsArgumentNullException(string fileName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _generator.GetFileName(fileName!));
        Assert.Equal("fileName", exception.ParamName);
    }

    [Theory]
    [InlineData("filename")]
    [InlineData("noextension")]
    [InlineData("file.")]
    public void GetFileName_WithoutExtension_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _generator.GetFileName(fileName));
        Assert.Equal("fileName", exception.ParamName);
        Assert.Contains("File must have an extension", exception.Message);
    }

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".pdf")]
    public void GetFileName_WithOnlyExtension_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _generator.GetFileName(fileName));
        Assert.Equal("fileName", exception.ParamName);
        Assert.Contains("File must have a valid name", exception.Message);
    }

    [Theory]
    [InlineData("   .jpg")]
    [InlineData("\t.png")]
    [InlineData("\n.pdf")]
    public void GetFileName_WithWhiteSpaceNameAndExtension_ThrowsArgumentException(string fileName)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _generator.GetFileName(fileName));
        Assert.Equal("fileName", exception.ParamName);
        Assert.Contains("File must have a valid name", exception.Message);
    }

    #endregion

    #region GetFileName Tests - Appendix Handling

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void GetFileName_WithNullOrWhiteSpaceAppendix_OmitsAppendixFromResult(string appendixName)
    {
        // Arrange
        const string fileName = "test.jpg";

        // Act
        var result = _generator.GetFileName(fileName, appendixName);

        // Assert
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}.jpg";

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("thumb")]
    [InlineData("large")]
    [InlineData("medium")]
    [InlineData("small")]
    public void GetFileName_WithValidAppendix_IncludesAppendixCorrectly(string appendixName)
    {
        // Arrange
        const string fileName = "test.jpg";

        // Act
        var result = _generator.GetFileName(fileName, appendixName);

        // Assert
        Assert.Contains($"-{appendixName}", result);

        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}-{appendixName}.jpg";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFileName_WithSpecialCharactersInAppendix_IncludesAppendixAsIs()
    {
        // Arrange
        const string fileName = "test.jpg";
        const string appendixName = "thumb_150x150";

        // Act
        var result = _generator.GetFileName(fileName, appendixName);

        // Assert
        Assert.Contains($"-{appendixName}", result);

        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}-{appendixName}.jpg";

        Assert.Equal(expected, result);
    }

    #endregion

    #region Interface Compliance Tests

    [Fact]
    public void DatedGuidFileNameGenerator_ImplementsIFileNameGenerator()
    {
        // Assert
        Assert.IsAssignableFrom<IFileNameGenerator>(_generator);
    }

    [Fact]
    public void IFileNameGenerator_NameProperty_ReturnsExpectedValue()
    {
        // Arrange
        IFileNameGenerator generator = _generator;

        // Act & Assert
        Assert.Equal(nameof(DatedGuidFileNameGenerator), generator.Name);
    }

    [Fact]
    public void IFileNameGenerator_GetFileName_WorksCorrectly()
    {
        // Arrange
        IFileNameGenerator generator = _generator;
        const string fileName = "test.jpg";

        // Act
        var result = generator.GetFileName(fileName);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.EndsWith(".jpg", result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetFileName_WithVeryLongFileName_HandlesCorrectly()
    {
        // Arrange
        var longFileName = new string('a', 100) + ".jpg";

        // Act
        var result = _generator.GetFileName(longFileName);

        // Assert
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}.jpg";

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetFileName_WithMultipleExtensions_UsesLastExtension()
    {
        // Arrange
        const string fileName = "backup.tar.gz";

        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        Assert.EndsWith(".gz", result);

        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var shortGuid = _testGuid.ToString("N")[..8];
        var expected = $"{dateStr}-{shortGuid}.gz";

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".JPEG")]
    [InlineData(".PNG")]
    [InlineData(".GIF")]
    public void GetFileName_WithDifferentCaseExtensions_NormalizesToLowerCase(string extension)
    {
        // Arrange
        var fileName = $"test{extension}";

        // Act
        var result = _generator.GetFileName(fileName);

        // Assert
        Assert.EndsWith(extension.ToLowerInvariant(), result);
    }

    #endregion
}