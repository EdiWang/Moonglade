namespace Moonglade.Data.DTO;

public record PostEditorMeta
{
    public string EditorChoice { get; set; }
    public string DefaultAuthor { get; set; }
    public int AbstractWords { get; set; }
    public List<CategoryBrief> Categories { get; set; }
    public List<LanguageInfo> Languages { get; set; }
}

public record CategoryBrief
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
}

public record LanguageInfo
{
    public string Value { get; set; }
    public string NativeName { get; set; }
}
