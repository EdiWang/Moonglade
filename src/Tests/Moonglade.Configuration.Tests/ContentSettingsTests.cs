using System.ComponentModel.DataAnnotations;

namespace Moonglade.Configuration.Tests;

public class ContentSettingsTests
{
    [Fact]
    public void Parse_WhenDefaultEditorIsMissing_UsesHtmlAndPreservesOtherSettings()
    {
        var (snapshot, _) = BlogConfig.Parse(new Dictionary<string, string>
        {
            [nameof(ContentSettings)] = """{"postListPageSize":20}"""
        });

        Assert.Equal(EditorChoice.Html, snapshot.ContentSettings.DefaultEditor);
        Assert.Equal(20, snapshot.ContentSettings.PostListPageSize);
    }

    [Theory]
    [InlineData(EditorChoice.Html)]
    [InlineData(EditorChoice.Markdown)]
    public void ToJson_RoundTripsDefaultEditor(EditorChoice editor)
    {
        var settings = new ContentSettings { DefaultEditor = editor };

        var restored = settings.ToJson().FromJson<ContentSettings>();

        Assert.Equal(editor, restored.DefaultEditor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void DefaultEditor_RejectsUndefinedValues(int value)
    {
        var settings = new ContentSettings { DefaultEditor = (EditorChoice)value };
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateProperty(
            settings.DefaultEditor,
            new ValidationContext(settings) { MemberName = nameof(ContentSettings.DefaultEditor) },
            results);

        Assert.False(valid);
        Assert.Single(results);
    }
}
