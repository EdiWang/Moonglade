namespace Moonglade.ImageStorage.Tests;

public class DatedGuidFileNameGeneratorTests
{
    private static readonly Guid TestGuid = new("12345678-1234-1234-1234-123456789012");
    private readonly DatedGuidFileNameGenerator _generator = new(TestGuid);

    [Fact]
    public void ExposesConfiguredIdentity()
    {
        Assert.Equal(TestGuid, _generator.UniqueId);
        Assert.Equal(nameof(DatedGuidFileNameGenerator), _generator.Name);
        Assert.IsAssignableFrom<IFileNameGenerator>(_generator);
    }

    [Theory]
    [InlineData("test.jpg", null, ".jpg")]
    [InlineData("IMAGE.JPG", "thumbnail", "-thumbnail.jpg")]
    [InlineData("backup.tar.GZ", "thumb_150x150", "-thumb_150x150.gz")]
    [InlineData("long-file-name.png", " ", ".png")]
    public void GetFileName_ReturnsDatedGuidName(string fileName, string appendix, string suffix)
    {
        var result = _generator.GetFileName(fileName, appendix);

        Assert.Equal($"{DateTime.UtcNow:yyyyMMdd}-12345678{suffix}", result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void GetFileName_RejectsBlankInput(string fileName)
    {
        Assert.ThrowsAny<ArgumentException>(() => _generator.GetFileName(fileName));
    }

    [Fact]
    public void GetFileName_RejectsNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => _generator.GetFileName(null!));
    }

    [Theory]
    [InlineData("filename", "File must have an extension")]
    [InlineData("file.", "File must have an extension")]
    [InlineData(".jpg", "File must have a valid name")]
    [InlineData(" .jpg", "File must have a valid name")]
    public void GetFileName_RejectsInvalidNames(string fileName, string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(() => _generator.GetFileName(fileName));

        Assert.Equal("fileName", exception.ParamName);
        Assert.Contains(expectedMessage, exception.Message);
    }
}
